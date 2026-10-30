import { CloudApi, type Config, hash, imageRequest } from "./cloud.ts";

const id = "e3ac9960-328c-4a62-8d68-1266361bb8cf";
const key = "test-only-not-a-real-secret";
const fixture = new Uint8Array(120);
fixture.set([255, 216, 255]);
fixture.set([255, 217], 118);
const config: Config = {
  url: "https://test.supabase.co",
  serviceKey: "fake-service",
  accessKeyHash: await hash(key),
  openAIKey: "fake-openai",
  galleryOrigin: "https://gallery.test",
  imageModel: "gpt-image-2.5-sunburst",
  responseModel: "gpt-6-luna",
};
function assert(value: unknown, message = "Assertion failed"): asserts value {
  if (!value) throw new Error(message);
}
function request(
  path: string,
  method = "GET",
  body?: unknown,
  authorized = true,
) {
  const bytes = body instanceof Uint8Array;
  return new Request("https://local.test/photobooth-api" + path, {
    method,
    headers: {
      ...(authorized ? { Authorization: "Bearer " + key } : {}),
      "Content-Type": bytes ? "image/jpeg" : "application/json",
    },
    body: body === undefined
      ? undefined
      : bytes
      ? new Uint8Array(body)
      : JSON.stringify(body),
  });
}

class FakeServices {
  sessions: Record<string, unknown>[] = [];
  jobs: Record<string, unknown>[] = [];
  assets: Record<string, unknown>[] = [];
  providerCreates = 0;
  providerMode = "completed";
  providerFail = false;
  claims = 0;
  storage = new Map<string, Uint8Array>();
  send: typeof fetch = (input, init) => {
    const url = new URL(String(input));
    const path = url.pathname;
    const method = init?.method ?? "GET";
    const body = typeof init?.body === "string"
      ? JSON.parse(init.body)
      : undefined;
    const reply = (data: unknown, status = 200) =>
      Promise.resolve(
        new Response(JSON.stringify(data), {
          status,
          headers: { "Content-Type": "application/json" },
        }),
      );
    if (url.hostname === "api.openai.com") {
      if (method === "POST") {
        this.providerCreates++;
        assert(
          body.background === true && body.tools[0].quality === "xhigh",
          "background xhigh request",
        );
        assert(
          body.input[0].content[1].image_url.startsWith(
            config.url + "/storage/v1/",
          ),
          "OpenAI reads the already uploaded original through a signed URL",
        );
        if (this.providerFail) throw new Error("Ambiguous network failure");
        return reply({ id: "resp_test", status: "queued" });
      }
      return reply({
        id: "resp_test",
        status: this.providerMode,
        output: [{
          type: "image_generation_call",
          result: btoa(String.fromCharCode(...fixture)),
        }],
      });
    }
    if (path.includes("/storage/v1/object/sign/")) {
      return reply({
        signedURL: "/object/sign/booth-private/signed.jpg?token=fake",
      });
    }
    if (path.includes("/storage/v1/object/")) {
      this.storage.set(path, new Uint8Array(init!.body as Uint8Array));
      return reply({});
    }
    if (path.endsWith("/rpc/booth_claim_job")) {
      let job = this.jobs.find((j) =>
        j.session_id === body.p_session && j.shot_index === body.p_index
      );
      if (job) return reply(false);
      job = {
        session_id: body.p_session,
        shot_index: body.p_index,
        state: "starting",
      };
      this.jobs.push(job);
      this.claims++;
      return reply(true);
    }
    const table = path.split("/").at(-1);
    const collection = table === "booth_sessions"
      ? this.sessions
      : table === "booth_jobs"
      ? this.jobs
      : this.assets;
    const matches = (row: Record<string, unknown>) =>
      Array.from(url.searchParams).every(([k, v]) =>
        k === "select" || String(row[k]) === v.replace(/^eq\./, "")
      );
    if (method === "GET") return reply(collection.filter(matches));
    if (method === "PATCH") {
      const selected = collection.filter(matches);
      selected.forEach((r) => Object.assign(r, body));
      return reply(selected);
    }
    if (method === "POST") {
      const duplicate = collection.find((r) =>
        table === "booth_sessions"
          ? r.id === body.id
          : r.session_id === body.session_id && r.name === body.name
      );
      if (!duplicate) {
        collection.push({
          ...body,
          expires_at: new Date(Date.now() + 86400000).toISOString(),
          share_hash: null,
          published_at: null,
          uploaded: false,
        });
      }
      return reply([]);
    }
    throw new Error("Unexpected fake request " + url);
  };
}

Deno.test("health and unauthorized requests never call external services", async () => {
  let calls = 0;
  const api = new CloudApi({ ...config, openAIKey: "" }, () => {
    calls++;
    throw new Error("No network allowed");
  });
  const health = await api.handle(request("/health", "GET", undefined, false));
  assert(health.status === 200);
  assert(!(await health.json()).image_generation_enabled);
  const blocked = await api.handle(
    request(
      "/sessions",
      "POST",
      { id, photo_count: 4, mode: "surprise" },
      false,
    ),
  );
  assert(blocked.status === 401);
  await blocked.body?.cancel();
  assert(calls === 0);
});
Deno.test("session idempotency preserves style and rejects conflicting options", async () => {
  const fake = new FakeServices();
  const api = new CloudApi(config, fake.send);
  const a = await (await api.handle(
    request("/sessions", "POST", { id, photo_count: 4, mode: "surprise" }),
  )).json();
  const b = await (await api.handle(
    request("/sessions", "POST", { id, photo_count: 4, mode: "surprise" }),
  )).json();
  assert(a.style === b.style && fake.sessions.length === 1);
  const bad = await api.handle(
    request("/sessions", "POST", { id, photo_count: 1, mode: "original" }),
  );
  assert(bad.status === 409);
  await bad.body?.cancel();
});
Deno.test("duplicate starts claim once; polling retrieves one completed image", async () => {
  const fake = new FakeServices();
  const api = new CloudApi(config, fake.send);
  await (await api.handle(
    request("/sessions", "POST", { id, photo_count: 1, mode: "surprise" }),
  )).body?.cancel();
  await (await api.handle(
    request(`/sessions/${id}/assets/original-1.jpg`, "PUT", fixture),
  )).body?.cancel();
  for (let i = 0; i < 2; i++) {
    await (await api.handle(
      request(`/sessions/${id}/photos/0`, "POST", {}),
    )).body?.cancel();
  }
  assert(fake.providerCreates === 1 && fake.claims === 1);
  const result = await (await api.handle(request(`/sessions/${id}/photos/0`)))
    .json();
  assert(result.state === "ready" && result.url.startsWith(config.url));
  assert(fake.storage.size === 2);
});
Deno.test("ambiguous start never automatically spends a second generation", async () => {
  const fake = new FakeServices();
  fake.providerFail = true;
  const api = new CloudApi(config, fake.send);
  await (await api.handle(
    request("/sessions", "POST", { id, photo_count: 1, mode: "surprise" }),
  )).body?.cancel();
  await (await api.handle(
    request(`/sessions/${id}/assets/original-1.jpg`, "PUT", fixture),
  )).body?.cancel();
  for (let i = 0; i < 2; i++) {
    await (await api.handle(
      request(`/sessions/${id}/photos/0`, "POST", {}),
    )).body?.cancel();
  }
  assert(fake.providerCreates === 1 && fake.jobs[0].state === "uncertain");
});
Deno.test("missing key rejects before claiming or calling OpenAI", async () => {
  const fake = new FakeServices();
  const api = new CloudApi({ ...config, openAIKey: "" }, fake.send);
  await (await api.handle(
    request("/sessions", "POST", { id, photo_count: 1, mode: "surprise" }),
  )).body?.cancel();
  const response = await api.handle(
    request(`/sessions/${id}/photos/0`, "POST", {}),
  );
  assert(response.status === 503);
  await response.body?.cancel();
  assert(fake.claims === 0 && fake.providerCreates === 0);
});
Deno.test("publish requires every asset; token stored only as hash; expired links fail", async () => {
  const fake = new FakeServices();
  const api = new CloudApi(config, fake.send);
  const token = "a".repeat(64);
  await (await api.handle(
    request("/sessions", "POST", { id, photo_count: 1, mode: "original" }),
  )).body?.cancel();
  const missing = await api.handle(
    request(`/sessions/${id}/share`, "POST", { token }),
  );
  assert(missing.status === 409);
  await missing.body?.cancel();
  for (const name of ["photo-1.jpg", "sheet.jpg", "archive.zip"]) {
    const bytes = name.endsWith("zip")
      ? new Uint8Array([80, 75, 3, 4, 0])
      : fixture;
    const response = await api.handle(
      request(`/sessions/${id}/assets/${name}`, "PUT", bytes),
    );
    assert(response.status === 201);
    await response.body?.cancel();
  }
  await (await api.handle(request(`/sessions/${id}/share`, "POST", { token })))
    .body?.cancel();
  assert(
    fake.sessions[0].share_hash === await hash(token) &&
      fake.sessions[0].share_hash !== token,
  );
  const found = await api.handle(
    request("/downloads/resolve", "POST", { token }, false),
  );
  assert(found.status === 200);
  assert((await found.json()).assets.length === 3);
  fake.sessions[0].expires_at = "2020-01-01T00:00:00Z";
  const expired = await api.handle(
    request("/downloads/resolve", "POST", { token }, false),
  );
  assert(expired.status === 404);
  await expired.body?.cancel();
});
Deno.test("asset hash conflict prevents replacing a reviewed photograph", async () => {
  const fake = new FakeServices();
  const api = new CloudApi(config, fake.send);
  await (await api.handle(
    request("/sessions", "POST", { id, photo_count: 1, mode: "original" }),
  )).body?.cancel();
  await (await api.handle(
    request(`/sessions/${id}/assets/photo-1.jpg`, "PUT", fixture),
  )).body?.cancel();
  const changed = fixture.slice();
  changed[10] = 9;
  const result = await api.handle(
    request(`/sessions/${id}/assets/photo-1.jpg`, "PUT", changed),
  );
  assert(result.status === 409);
  await result.body?.cancel();
});
Deno.test("payload fixes one edit per pose and preserves original separately", () => {
  const payload = imageRequest(
    config,
    "https://test.supabase.co/storage/original.jpg?token=temporary",
    "cinema",
  );
  assert(payload.max_tool_calls === 1 && payload.tools[0].action === "edit");
  assert(
    payload.tools[0].model === "gpt-image-2.5-sunburst" &&
      payload.tools[0].quality === "xhigh",
  );
});

Deno.test("missing original blocks a paid generation; surprise sharing requires originals", async () => {
  const fake = new FakeServices();
  const api = new CloudApi(config, fake.send);
  await (await api.handle(
    request("/sessions", "POST", { id, photo_count: 1, mode: "surprise" }),
  )).body?.cancel();
  const blocked = await api.handle(
    request(`/sessions/${id}/photos/0`, "POST", {}),
  );
  assert(
    blocked.status === 409 && fake.providerCreates === 0 && fake.claims === 0,
  );
  await blocked.body?.cancel();
  for (const name of ["photo-1.jpg", "sheet.jpg", "archive.zip"]) {
    await (await api.handle(
      request(
        `/sessions/${id}/assets/${name}`,
        "PUT",
        name.endsWith("zip") ? new Uint8Array([80, 75, 3, 4, 0]) : fixture,
      ),
    )).body?.cancel();
  }
  const missing = await api.handle(
    request(`/sessions/${id}/share`, "POST", { token: "b".repeat(64) }),
  );
  assert(missing.status === 409);
  await missing.body?.cancel();
  await (await api.handle(
    request(`/sessions/${id}/assets/original-1.jpg`, "PUT", fixture),
  )).body?.cancel();
  const ready = await api.handle(
    request(`/sessions/${id}/share`, "POST", { token: "b".repeat(64) }),
  );
  assert(ready.status === 200);
  await ready.body?.cancel();
  const resolved = await api.handle(
    request("/downloads/resolve", "POST", { token: "b".repeat(64) }, false),
  );
  assert(
    (await resolved.json()).assets.some((a: { name: string }) =>
      a.name === "original-1.jpg"
    ),
  );
});
