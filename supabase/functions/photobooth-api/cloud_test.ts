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
  metrics: Record<string, unknown>[] = [];
  metricsFail = false;
  providerCreates = 0;
  providerMode = "completed";
  providerFail = false;
  providerRateLimited = false;
  providerGate?: Promise<void>;
  claims = 0;
  claimError = "";
  storage = new Map<string, Uint8Array>();
  send: typeof fetch = async (input, init) => {
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
          path === "/v1/images/edits" && body.model === "gpt-image-2.5-sunburst" && body.quality === "medium" && body.size === "1024x1024" && body.n === 1,
          "direct medium square edit, no mainline model or Responses call",
        );
        assert(
          body.images[0].image_url.startsWith(
            config.url + "/storage/v1/",
          ),
          "OpenAI reads the already uploaded original through a signed URL",
        );
        if (this.providerFail) throw new Error("Ambiguous network failure");
        if (this.providerRateLimited) return reply({ error: "rate limited" }, 429);
        if (this.providerGate) await this.providerGate;
        return reply({ data: [{ b64_json: btoa(String.fromCharCode(...fixture)) }], usage: { total_tokens: 100 } });
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
      if (method === "GET") {
        const stored = this.storage.get(path.replace("/object/authenticated/", "/object/"));
        return Promise.resolve(new Response(stored ? new Uint8Array(stored) : null, { status: stored ? 200 : 404 }));
      }
      this.storage.set(path, new Uint8Array(init!.body as Uint8Array));
      return reply({});
    }
    if (path.endsWith("/rpc/booth_metrics_ingest")) {
      if(this.metricsFail) return reply({},503);
      for(const e of body.p_events) if(!this.metrics.some(m=>m.id===e.id))this.metrics.push(e);
      return reply(body.p_events.length);
    }
    if (path.endsWith("/rpc/booth_claim_job")) {
      if (this.claimError) return reply({ code: "P0001", message: this.claimError }, 400);
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
        k === "select" || (k === "or" ? row.share_hash === null || v.includes(`share_hash.eq.${row.share_hash}`) : String(row[k]) === v.replace(/^eq\./, ""))
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
Deno.test("early gallery resolves only its available photos without authorizing generation", async () => {
  const fake = new FakeServices(); const api = new CloudApi(config, fake.send);
  await (await api.handle(request("/sessions", "POST", { id, photo_count: 4, mode: "surprise" }))).body?.cancel();
  const token = "b".repeat(64);
  const early = await api.handle(request(`/sessions/${id}/gallery`, "POST", { token }));
  assert(early.status === 200); assert((await early.json()).gallery_url.endsWith("/#" + token));
  assert(fake.sessions[0].share_hash === await hash(token) && fake.sessions[0].published_at === null);
  const empty = await (await api.handle(request("/downloads/resolve", "POST", { token }, false))).json();
  assert(empty.state === "processing" && empty.assets.length === 0 && fake.providerCreates === 0);
  for (const name of ["original-1.jpg", "photo-1.jpg"]) {
    const upload = await api.handle(request(`/sessions/${id}/assets/${name}`, "PUT", fixture));
    assert(upload.status === 201); await upload.body?.cancel();
  }
  const partial = await (await api.handle(request("/downloads/resolve", "POST", { token }, false))).json();
  assert(partial.state === "processing" && partial.completed_count === 1 && partial.assets.length === 2);
  assert(!JSON.stringify(partial).includes("owner_hash") && !JSON.stringify(partial).includes("response_id"));
  const conflict = await api.handle(request(`/sessions/${id}/gallery`, "POST", { token: "c".repeat(64) }));
  assert(conflict.status === 409); await conflict.body?.cancel();
  const forbidden = await api.handle(new Request(`https://local.test/photobooth-api/sessions/${id}/photos/0`, {method:"POST",headers:{Authorization:"Bearer " + token},body:"{}"}));
  assert(forbidden.status === 401); await forbidden.body?.cancel();
  fake.jobs.push({session_id:id,shot_index:0,state:"failed"});
  const failed = await (await api.handle(request("/downloads/resolve", "POST", {token}, false))).json();
  assert(failed.state === "failed" && failed.assets.length === 2 && fake.providerCreates === 0);
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
  const shared = await (await api.handle(request(`/sessions/${id}/share`, "POST", { token }))).json();
  assert(shared.gallery_url === config.galleryOrigin + "/#" + token);
  assert(
    fake.sessions[0].share_hash === await hash(token) &&
      fake.sessions[0].share_hash !== token,
  );
  const found = await api.handle(
    request("/downloads/resolve", "POST", { token }, false),
  );
  assert(found.status === 200);
  const resolved = await found.json();
  assert(resolved.assets.length === 3 && resolved.mode === "original" && resolved.photo_count === 1 && resolved.used_original_fallback === false);
  assert(fake.providerCreates === 0);
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
Deno.test("explicit fallback publishes originals without replacing a partial transformation", async () => {
  const fake = new FakeServices(); const api = new CloudApi(config, fake.send); const token = "d".repeat(64);
  await (await api.handle(request("/sessions", "POST", {id,photo_count:4,mode:"surprise"}))).body?.cancel();
  await (await api.handle(request(`/sessions/${id}/gallery`, "POST", {token}))).body?.cancel();
  for(const name of ["photo-1.jpg","original-1.jpg","original-2.jpg","original-3.jpg","original-4.jpg","sheet.jpg","archive.zip"]) {
    const bytes=name.endsWith("zip")?new Uint8Array([80,75,3,4,0]):fixture;
    const upload=await api.handle(request(`/sessions/${id}/assets/${name}`,"PUT",bytes));
    assert(upload.status===201);await upload.body?.cancel();
  }
  const published=await api.handle(request(`/sessions/${id}/share`,"POST",{token,used_original_fallback:true}));
  assert(published.status===200);await published.body?.cancel();
  const resolved=await (await api.handle(request("/downloads/resolve","POST",{token},false))).json();
  assert(resolved.state==="ready"&&resolved.used_original_fallback===true&&resolved.assets.length===6);
  assert(resolved.assets.every((asset:{name:string})=>!asset.name.startsWith("photo-"))&&fake.providerCreates===0);
});
Deno.test("server assembles the complete archive without a booth ZIP upload", async () => {
  const fake = new FakeServices(); const api = new CloudApi(config, fake.send); const token = "e".repeat(64);
  await (await api.handle(request("/sessions", "POST", {id,photo_count:1,mode:"original"}))).body?.cancel();
  for (const name of ["photo-1.jpg", "sheet.jpg"]) {
    const response=await api.handle(request(`/sessions/${id}/assets/${name}`,"PUT",fixture));
    assert(response.status===201);await response.body?.cancel();
  }
  const share=await api.handle(request(`/sessions/${id}/share`,"POST",{token}));
  assert(share.status===200);await share.body?.cancel();
  const archive=fake.storage.get(`/storage/v1/object/booth-private/${id}/archive.zip`)!;
  assert(archive.length>fixture.length*2&&new DataView(archive.buffer).getUint32(0,true)===0x04034b50);
  const digest=await hash(archive);
  await (await api.handle(request(`/sessions/${id}/share`,"POST",{token}))).body?.cancel();
  assert(await hash(fake.storage.get(`/storage/v1/object/booth-private/${id}/archive.zip`)!)===digest&&fake.providerCreates===0);
});
Deno.test("optimized input starts a job; direct result appears without a booth reupload", async () => {
  const fake=new FakeServices();const api=new CloudApi(config,fake.send);const token="f".repeat(64);
  await (await api.handle(request("/sessions","POST",{id,photo_count:1,mode:"surprise"}))).body?.cancel();
  await (await api.handle(request(`/sessions/${id}/gallery`,"POST",{token}))).body?.cancel();
  const input=await api.handle(request(`/sessions/${id}/assets/input-1.jpg`,"PUT",fixture));
  assert(input.status===201);await input.body?.cancel();
  const start=await api.handle(request(`/sessions/${id}/photos/0`,"POST",{}));
  assert(start.status===202);await start.body?.cancel();
  const result=await (await api.handle(request(`/sessions/${id}/photos/0?transport=direct`))).json();
  assert(result.state==="ready"&&result.asset_stored===true&&result.sha256===await hash(fixture));
  const gallery=await (await api.handle(request("/downloads/resolve","POST",{token},false))).json();
  assert(gallery.assets.length===1&&gallery.assets[0].name==="photo-1.jpg"&&fake.providerCreates===1);
  const repeated=await (await api.handle(request(`/sessions/${id}/photos/0?transport=direct`))).json();
  assert(repeated.asset_stored===true&&repeated.sha256===result.sha256&&fake.providerCreates===1);
});
Deno.test("explicit provider rate rejection frees the in-flight slot without retrying a start", async () => {
 const fake=new FakeServices();fake.providerRateLimited=true;const api=new CloudApi(config,fake.send);
 await (await api.handle(request("/sessions","POST",{id,photo_count:1,mode:"surprise"}))).body?.cancel();
 await (await api.handle(request(`/sessions/${id}/assets/original-1.jpg`,"PUT",fixture))).body?.cancel();
 const response=await api.handle(request(`/sessions/${id}/photos/0`,"POST",{}));
 assert(response.status===202);await response.body?.cancel();
 const status=await (await api.handle(request(`/sessions/${id}/photos/0`))).json();
 assert(status.state==="failed"&&status.code==="provider_rate_limited");
 assert(fake.jobs[0].state==="failed"&&fake.providerCreates===1);
});
Deno.test("payload fixes one edit per pose and preserves original separately", () => {
  const payload = imageRequest(
    config,
    "https://test.supabase.co/storage/original.jpg?token=temporary",
    "cinema",
  );
  assert(payload.n === 1 && !("tools" in payload) && !("background" in payload));
  assert(
    payload.model === "gpt-image-2.5-sunburst" && payload.quality === "medium" && payload.size === "1024x1024",
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

Deno.test("public catalog is immediate, safe metadata without prompts or provider calls", async () => {
  const fake = new FakeServices();
  const api = new CloudApi(config, fake.send);
  const response = await api.handle(request("/styles", "GET", undefined, false));
  assert(response.status === 200);
  const data = await response.json();
  assert(data.styles.length === 6 && data.version);
  assert(!JSON.stringify(data).includes('"prompt"'));
  assert(data.styles.every((s: { label: string; description: string; phrases: string[] }) =>
    !/[\u2013\u2014-]/.test([s.label, s.description, ...s.phrases].join(" "))));
  assert(fake.providerCreates === 0 && fake.sessions.length === 0);
});
Deno.test("explicit universe and prompt freeze across updates; rejected changes do not generate", async () => {
  const fake = new FakeServices(); const api = new CloudApi(config, fake.send);
  const selected = { id, photo_count: 4, mode: "surprise", style: "halloween" };
  const first = await api.handle(request("/sessions", "POST", selected));
  assert(first.status === 201 && (await first.json()).style === "halloween");
  assert(String(fake.sessions[0].style_prompt).includes("citrouille"));
  fake.sessions[0].style_prompt = "Frozen test scene";
  const resumed = await api.handle(request("/sessions", "POST", selected));
  assert(resumed.status === 201); await resumed.body?.cancel();
  assert(fake.sessions[0].style_prompt === "Frozen test scene");
  const changed = await api.handle(request("/sessions", "POST", { ...selected, style: "space" }));
  assert(changed.status === 409); await changed.body?.cancel();
  const unknown = await api.handle(request("/sessions", "POST", { ...selected, id: crypto.randomUUID(), style: "unknown_style" }));
  assert(unknown.status === 400); await unknown.body?.cancel();
  assert(fake.providerCreates === 0);
  const payload = imageRequest(config, "https://test/image.jpg", "removed_theme", "Frozen test scene");
  const prompt = payload.prompt;
  assert(prompt.includes("Frozen test scene") && prompt.includes("corpulence") && prompt.includes("aides à la mobilité"));
});

Deno.test("every mutation rejects outsiders and gallery capabilities before all external calls", async () => {
  let calls = 0;
  const api = new CloudApi(config, () => { calls++; throw new Error("Forbidden network"); });
  for (const [path, method] of [["/sessions", "POST"], [`/sessions/${id}/photos/0`, "POST"], [`/sessions/${id}/assets/photo-1.jpg`, "PUT"], [`/sessions/${id}/share`, "POST"], ["/booth/check", "GET"]]) {
    for (const bearer of ["", "a".repeat(64), "fake-openai", "fake-service"]) {
      const response = await api.handle(new Request("https://local.test/photobooth-api" + path, {method, headers: {Authorization: "Bearer " + bearer}}));
      assert(response.status === 401); await response.body?.cancel();
    }
  }
  assert(calls === 0);
});
Deno.test("server limits reject before any provider call and expose only safe codes", async () => {
  for (const code of ["event_quota_reached", "generation_rate_limited", "generation_busy", "generation_disabled", "sensitive SQL details"]) {
    const fake = new FakeServices(); const api = new CloudApi(config, fake.send);
    await (await api.handle(request("/sessions", "POST", { id, photo_count: 1, mode: "surprise" }))).body?.cancel();
    await (await api.handle(request(`/sessions/${id}/assets/original-1.jpg`, "PUT", fixture))).body?.cancel();
    fake.claimError = code;
    const response = await api.handle(request(`/sessions/${id}/photos/0`, "POST", {}));
    assert(response.status === (code === "generation_disabled" || code === "sensitive SQL details" ? 503 : 429));
    assert((await response.json()).code === (code === "sensitive SQL details" ? "database_unavailable" : code));
    assert(fake.providerCreates === 0 && fake.claims === 0);
  }
});
Deno.test("parallel duplicate starts cannot create two provider requests", async () => {
  const fake = new FakeServices(); const api = new CloudApi(config, fake.send);
  await (await api.handle(request("/sessions", "POST", { id, photo_count: 1, mode: "surprise" }))).body?.cancel();
  await (await api.handle(request(`/sessions/${id}/assets/original-1.jpg`, "PUT", fixture))).body?.cancel();
  const responses = await Promise.all(Array.from({length: 12}, () => api.handle(request(`/sessions/${id}/photos/0`, "POST", {}))));
  for (const response of responses) { assert(response.status === 202); await response.body?.cancel(); }
  assert(fake.providerCreates === 1);
});

Deno.test("four direct edits return immediately and complete in the Edge background", async () => {
  const fake = new FakeServices(); let finish!: () => void;
  fake.providerGate = new Promise<void>(resolve => { finish = resolve; });
  const tasks: Promise<void>[] = [];
  const api = new CloudApi(config, fake.send, task => { tasks.push(task); });
  await (await api.handle(request("/sessions", "POST", { id, photo_count: 4, mode: "surprise" }))).body?.cancel();
  try {
    for (let i = 0; i < 4; i++) {
      await (await api.handle(request(`/sessions/${id}/assets/input-${i + 1}.jpg`, "PUT", fixture))).body?.cancel();
      const response = await api.handle(request(`/sessions/${id}/photos/${i}`, "POST", {}));
      assert(response.status === 202); await response.body?.cancel();
    }
    assert(tasks.length === 4 && fake.providerCreates === 4 && fake.jobs.every(j => j.state === "running"));
    const poll = await (await api.handle(request(`/sessions/${id}/photos/0`))).json();
    assert(poll.state === "running" && fake.providerCreates === 4);
    await (await api.handle(request(`/sessions/${id}/photos/0`, "POST", {}))).body?.cancel();
    assert(fake.providerCreates === 4, "duplicate while running never creates a fifth edit");
  } finally { finish(); await Promise.all(tasks); }
  assert(fake.jobs.every(j => j.state === "ready" && !j.response_id));
  assert(fake.assets.filter(a => String(a.name).startsWith("photo-") && a.uploaded).length === 4);
});

Deno.test("an interrupted direct worker is uncertain and cannot be relaunched", async () => {
  const fake = new FakeServices(); const api = new CloudApi(config, fake.send);
  await (await api.handle(request("/sessions", "POST", { id, photo_count: 1, mode: "surprise" }))).body?.cancel();
  fake.jobs.push({ session_id: id, shot_index: 0, state: "running", usage: { provider: "images" }, updated_at: new Date(Date.now() - 240000).toISOString() });
  const status = await (await api.handle(request(`/sessions/${id}/photos/0`))).json();
  assert(status.state === "uncertain" && fake.providerCreates === 0);
  await (await api.handle(request(`/sessions/${id}/assets/input-1.jpg`, "PUT", fixture))).body?.cancel();
  await (await api.handle(request(`/sessions/${id}/photos/0`, "POST", {}))).body?.cancel();
  assert(fake.providerCreates === 0 && fake.jobs[0].state === "uncertain");
});

Deno.test("stored direct output survives a lost completion update; old Responses jobs can finish", async () => {
  const fake = new FakeServices(); const api = new CloudApi(config, fake.send);
  await (await api.handle(request("/sessions", "POST", { id, photo_count: 4, mode: "surprise" }))).body?.cancel();
  await (await api.handle(request(`/sessions/${id}/assets/photo-1.jpg`, "PUT", fixture))).body?.cancel();
  fake.jobs.push({ session_id: id, shot_index: 0, state: "uncertain", usage: { provider: "images" } });
  const restored = await (await api.handle(request(`/sessions/${id}/photos/0`))).json();
  assert(restored.state === "ready" && restored.asset_stored && fake.providerCreates === 0);
  fake.jobs.push({ session_id: id, shot_index: 1, state: "running", response_id: "resp_test" });
  const legacy = await (await api.handle(request(`/sessions/${id}/photos/1?transport=direct`))).json();
  assert(legacy.state === "ready" && legacy.asset_stored && fake.providerCreates === 0);
});
Deno.test("monitoring requires booth auth, validates dimensions and never reaches OpenAI", async () => {
  const f=new FakeServices();const api=new CloudApi(config,f.send);
  const event={id:crypto.randomUUID(),session_id:id,kind:"capture_saved",occurred_at:new Date().toISOString(),mode:"surprise",photo_count:4,requested_style:"chef",source:"fixture",shot_index:0};
  assert((await api.handle(request("/monitoring/booth","POST",{events:[event]},false))).status===401);
  assert(Number(f.metrics.length)===0);
  assert((await api.handle(request("/monitoring/booth","POST",{events:[{...event,source:"provider"}]}))).status===400);
  assert((await api.handle(request("/monitoring/booth","POST",{events:[{...event,kind:"upload",asset:"https://secret.invalid/key",duration_ms:10,bytes:100,outcome:"success"}]}))).status===400);
  assert((await api.handle(request("/monitoring/booth","POST",{events:[event]}))).status===200);
  assert(Number(f.metrics.length)===1 && f.metrics[0].origin==="booth" && f.metrics[0].source==="fixture");
  assert((await api.handle(request("/monitoring/booth","POST",{events:[event]}))).status===200);
  assert(Number(f.metrics.length)===1 && f.providerCreates===0);
});
Deno.test("guest counters validate capability and uploaded public assets without generation", async()=>{
  const f=new FakeServices();const api=new CloudApi(config,f.send);
  await api.handle(request("/sessions","POST",{id,photo_count:1,mode:"surprise",style:"halloween"}));
  const token="a".repeat(64);
  await api.handle(request(`/sessions/${id}/gallery`,"POST",{token}));
  await api.handle(request(`/sessions/${id}/assets/input-1.jpg`,"PUT",fixture));
  await api.handle(request(`/sessions/${id}/assets/original-1.jpg`,"PUT",fixture));
  const event={id:crypto.randomUUID(),view_id:crypto.randomUUID(),kind:"gallery_view"};
  assert((await api.handle(request("/downloads/events","POST",{token:"b".repeat(64),events:[event]},false))).status===404);
  assert((await api.handle(request("/downloads/events","POST",{token,events:[event]},false))).status===200);
  assert((await api.handle(request("/downloads/events","POST",{token,events:[{...event,id:crypto.randomUUID(),kind:"download_requested",asset:"input-1.jpg"}]},false))).status===400);
  assert((await api.handle(request("/downloads/events","POST",{token,events:[{...event,id:crypto.randomUUID(),kind:"download_requested",asset:"photo-1.jpg"}]},false))).status===400);
  assert((await api.handle(request("/downloads/events","POST",{token,events:[{...event,id:crypto.randomUUID(),kind:"download_requested",asset:"original-1.jpg"}]},false))).status===200);
  assert(Number(f.metrics.length)===2 && f.metrics.every(e=>e.origin==="guest"));assert(f.providerCreates===0);
});
Deno.test("a monitoring outage cannot turn a successful paid generation into failure",async()=>{
  const f=new FakeServices();const api=new CloudApi(config,f.send);f.metricsFail=true;
  await api.handle(request("/sessions","POST",{id,photo_count:1,mode:"surprise",style:"halloween"}));
  await api.handle(request(`/sessions/${id}/assets/input-1.jpg`,"PUT",fixture));
  assert((await api.handle(request(`/sessions/${id}/photos/0`,"POST",{}))).status===202);
  assert(f.jobs[0].state==="ready" && f.providerCreates===1);
  assert(typeof (f.jobs[0].usage as Record<string,unknown>).provider_elapsed_ms==="number");
});
