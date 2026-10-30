import { Buffer } from "node:buffer";

export type Config = {
  url: string;
  serviceKey: string;
  accessKeyHash: string;
  openAIKey: string;
  galleryOrigin: string;
  imageModel: string;
  responseModel: string;
};
export type SessionRow = {
  id: string;
  owner_hash: string;
  photo_count: number;
  mode: string;
  style: string;
  share_hash: string | null;
  published_at: string | null;
  expires_at: string;
};
type Job = {
  state: string;
  response_id?: string;
  object_key?: string;
  updated_at?: string;
};
type Asset = {
  name: string;
  object_key: string;
  sha256: string;
  bytes: number;
  uploaded: boolean;
};
type ImageCall = { type: string; result?: string; usage?: unknown };
type ProviderResponse = {
  id: string;
  status: string;
  output?: ImageCall[];
  usage?: unknown;
};

export class ApiError extends Error {
  constructor(public status: number, public code: string) {
    super(code);
  }
}
export const styles = {
  cinema:
    "Un portrait de cinéma d’aventure élégant, décor de première de film, lumière dorée douce.",
  space:
    "Une fête rétrofuturiste dans une station spatiale, couleurs joyeuses et tenues conservées.",
  garden:
    "Un jardin enchanté de fête, fleurs lumineuses, ambiance de conte photographique.",
  painting:
    "Un portrait peint à la gouache expressive, aplats colorés, visages fidèles et reconnaissables.",
  disco:
    "Une fête disco des années 70, reflets de boule à facettes, ambiance chaleureuse.",
};
export function imageRequest(config: Config, imageUrl: string, style: string) {
  if (!(style in styles)) throw new ApiError(400, "invalid_style");
  return {
    model: config.responseModel,
    background: true,
    store: true,
    tool_choice: { type: "image_generation" },
    parallel_tool_calls: false,
    max_tool_calls: 1,
    input: [{
      role: "user",
      content: [
        {
          type: "input_text",
          text: "Transforme cette seule photo pour un anniversaire familial. " +
            styles[style as keyof typeof styles] +
            " Conserve exactement le nombre de personnes, leur identité, âge apparent, visage et pose. " +
            "Garde toutes les personnes entièrement dans le cadre, sans en ajouter. Ne sexualise personne. Pas de texte, logo, collage ni planche. Une seule image paysage.",
        },
        {
          type: "input_image",
          image_url: imageUrl,
        },
      ],
    }],
    tools: [{
      type: "image_generation",
      model: config.imageModel,
      action: "edit",
      quality: "xhigh",
      size: "1536x1024",
      output_format: "jpeg",
      output_compression: 95,
    }],
  };
}

export async function hash(value: string | Uint8Array): Promise<string> {
  const bytes = typeof value === "string"
    ? new TextEncoder().encode(value)
    : value;
  return Buffer.from(
    await crypto.subtle.digest("SHA-256", new Uint8Array(bytes)),
  ).toString("hex");
}
function equalHash(a: string, b: string): boolean {
  if (a.length !== 64 || b.length !== 64) return false;
  let diff = 0;
  for (let i = 0; i < 64; i++) diff |= a.charCodeAt(i) ^ b.charCodeAt(i);
  return diff === 0;
}
export async function readBytes(
  request: Request,
  maximum: number,
): Promise<Uint8Array> {
  if (Number(request.headers.get("content-length")) > maximum) {
    throw new ApiError(413, "file_too_large");
  }
  const reader = request.body?.getReader();
  if (!reader) throw new ApiError(400, "empty_body");
  const chunks: Uint8Array[] = [];
  let size = 0;
  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    size += value.length;
    if (size > maximum) {
      await reader.cancel();
      throw new ApiError(413, "file_too_large");
    }
    chunks.push(value);
  }
  const result = new Uint8Array(size);
  let offset = 0;
  for (const chunk of chunks) {
    result.set(chunk, offset);
    offset += chunk.length;
  }
  return result;
}
function jpeg(bytes: Uint8Array) {
  if (
    bytes.length < 100 || bytes[0] !== 255 || bytes[1] !== 216 ||
    bytes[2] !== 255 ||
    bytes.at(-2) !== 255 || bytes.at(-1) !== 217
  ) throw new ApiError(400, "invalid_jpeg");
}
async function jsonBody(request: Request): Promise<Record<string, unknown>> {
  try {
    const value = JSON.parse(
      new TextDecoder().decode(await readBytes(request, 8192)),
    );
    if (value === null || typeof value !== "object" || Array.isArray(value)) {
      throw new ApiError(400, "invalid_json");
    }
    return value;
  } catch (e) {
    if (e instanceof ApiError) throw e;
    throw new ApiError(400, "invalid_json");
  }
}

export class CloudApi {
  constructor(
    readonly config: Config,
    private readonly send: typeof fetch = fetch,
  ) {}

  private async rest<T>(
    path: string,
    method = "GET",
    body?: unknown,
    prefer = "return=representation",
  ): Promise<T> {
    const response = await this.send(this.config.url + "/rest/v1/" + path, {
      method,
      headers: {
        apikey: this.config.serviceKey,
        Authorization: "Bearer " + this.config.serviceKey,
        "Content-Type": "application/json",
        Prefer: prefer,
      },
      body: body === undefined ? undefined : JSON.stringify(body),
      signal: AbortSignal.timeout(20000),
    });
    if (!response.ok) throw new ApiError(503, "database_unavailable");
    const text = await response.text();
    return (text ? JSON.parse(text) : null) as T;
  }
  private async session(id: string, owner: string) {
    const rows = await this.rest<SessionRow[]>(
      `booth_sessions?id=eq.${id}&owner_hash=eq.${owner}&select=*`,
    );
    if (!rows.length) throw new ApiError(404, "session_not_found");
    if (new Date(rows[0].expires_at).getTime() <= Date.now()) {
      throw new ApiError(410, "session_expired");
    }
    return rows[0];
  }
  private async job(id: string, index: number) {
    return (await this.rest<Job[]>(
      `booth_jobs?session_id=eq.${id}&shot_index=eq.${index}&select=*`,
    ))[0];
  }
  private updateJob(
    id: string,
    index: number,
    changes: Partial<Job> & { usage?: unknown },
  ) {
    return this.rest(
      `booth_jobs?session_id=eq.${id}&shot_index=eq.${index}`,
      "PATCH",
      { ...changes, updated_at: new Date().toISOString() },
    );
  }
  private async upload(key: string, bytes: Uint8Array, contentType: string) {
    const response = await this.send(
      `${this.config.url}/storage/v1/object/booth-private/${key}`,
      {
        method: "POST",
        headers: {
          Authorization: "Bearer " + this.config.serviceKey,
          apikey: this.config.serviceKey,
          "Content-Type": contentType,
          "x-upsert": "true",
        },
        body: new Uint8Array(bytes),
        signal: AbortSignal.timeout(30000),
      },
    );
    if (!response.ok) throw new ApiError(503, "storage_unavailable");
    await response.body?.cancel();
  }
  private async signed(key: string, expiresIn = 300) {
    const response = await this.send(
      `${this.config.url}/storage/v1/object/sign/booth-private/${key}`,
      {
        method: "POST",
        headers: {
          Authorization: "Bearer " + this.config.serviceKey,
          apikey: this.config.serviceKey,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ expiresIn }),
        signal: AbortSignal.timeout(20000),
      },
    );
    if (!response.ok) throw new ApiError(503, "storage_unavailable");
    const data = await response.json();
    return this.config.url + "/storage/v1" + data.signedURL;
  }

  async handle(request: Request): Promise<Response> {
    const headers = {
      "Content-Type": "application/json; charset=utf-8",
      "Cache-Control": "no-store",
      "X-Content-Type-Options": "nosniff",
      "Referrer-Policy": "no-referrer",
      "Access-Control-Allow-Origin": this.config.galleryOrigin,
      "Access-Control-Allow-Headers": "authorization, content-type",
      "Access-Control-Allow-Methods": "GET,POST,PUT,OPTIONS",
    };
    const reply = (data: unknown, status = 200) =>
      new Response(JSON.stringify(data), { status, headers });
    try {
      const path = new URL(request.url).pathname.replace(
        /^.*\/photobooth-api/,
        "",
      ).replace(/\/$/, "");
      if (request.method === "OPTIONS") {
        return new Response(null, { status: 204, headers });
      }
      if (path === "/health") {
        if (request.method !== "GET") {
          throw new ApiError(405, "method_not_allowed");
        }
        return reply({
          service: "photobooth",
          api_version: "0.2.0",
          stage: "rehearsal",
          production_ready: false,
          session_api_enabled: !!this.config.accessKeyHash,
          image_generation_enabled: !!this.config.openAIKey &&
            !!this.config.accessKeyHash,
        });
      }
      if (path === "/downloads/resolve" && request.method === "POST") {
        const { token } = await jsonBody(request);
        if (typeof token !== "string" || !/^[a-f0-9]{64}$/.test(token)) {
          throw new ApiError(404, "link_unavailable");
        }
        const rows = await this.rest<SessionRow[]>(
          `booth_sessions?share_hash=eq.${await hash(token)}&select=*`,
        );
        const row = rows[0];
        if (
          !row || !row.published_at || Date.parse(row.expires_at) <= Date.now()
        ) throw new ApiError(404, "link_unavailable");
        const assets = await this.rest<Asset[]>(
          `booth_assets?session_id=eq.${row.id}&uploaded=eq.true&select=*`,
        );
        return reply({
          expires_at: row.expires_at,
          assets: await Promise.all(
            assets.map(async (a) => ({
              name: a.name,
              url: await this.signed(a.object_key),
            })),
          ),
        });
      }
      const bearer =
        request.headers.get("authorization")?.replace(/^Bearer /, "") ?? "";
      if (
        !bearer || !equalHash(await hash(bearer), this.config.accessKeyHash)
      ) throw new ApiError(401, "unauthorized");
      const owner = this.config.accessKeyHash;
      if (path === "/booth/check" && request.method === "GET") {
        return reply({
          authenticated: true,
          image_generation_enabled: !!this.config.openAIKey,
        });
      }
      if (path === "/sessions" && request.method === "POST") {
        const input = await jsonBody(request);
        if (
          typeof input.id !== "string" || !uuid(input.id) ||
          typeof input.photo_count !== "number" ||
          ![1, 4].includes(input.photo_count) ||
          !["original", "surprise"].includes(String(input.mode))
        ) throw new ApiError(400, "invalid_session");
        const styleKeys = Object.keys(styles);
        const random = crypto.getRandomValues(new Uint32Array(1))[0];
        await this.rest("booth_sessions", "POST", {
          id: input.id,
          owner_hash: owner,
          photo_count: input.photo_count,
          mode: input.mode,
          style: styleKeys[random % styleKeys.length],
        }, "resolution=ignore-duplicates,return=representation");
        const session = await this.session(input.id, owner);
        if (
          session.photo_count !== input.photo_count ||
          session.mode !== input.mode
        ) throw new ApiError(409, "session_conflict");
        return reply({
          id: session.id,
          style: session.style,
          expires_at: session.expires_at,
        }, 201);
      }
      const match = path.match(
        /^\/sessions\/([a-f0-9-]{36})\/(photos\/([0-3])|assets\/([^/]+)|share)$/,
      );
      if (!match || !uuid(match[1])) throw new ApiError(404, "not_found");
      const id = match[1];
      const session = await this.session(id, owner);
      if (match[3] !== undefined) {
        const index = Number(match[3]);
        if (index >= session.photo_count || session.mode !== "surprise") {
          throw new ApiError(400, "invalid_shot");
        }
        if (request.method === "POST") {
          if (!this.config.openAIKey) {
            throw new ApiError(503, "openai_not_configured");
          }
          await jsonBody(request);
          const originals = await this.rest<Asset[]>(
            `booth_assets?session_id=eq.${id}&name=eq.original-${
              index + 1
            }.jpg&uploaded=eq.true&select=*`,
          );
          if (originals.length !== 1) {
            throw new ApiError(409, "original_required");
          }
          const imageUrl = await this.signed(originals[0].object_key, 1800);
          const claimed = await this.rest<boolean>(
            "rpc/booth_claim_job",
            "POST",
            { p_session: id, p_index: index, p_owner: owner },
          );
          if (!claimed) {
            return reply({
              state: (await this.job(id, index))?.state ?? "uncertain",
            }, 202);
          }
          try {
            const response = await this.send(
              "https://api.openai.com/v1/responses",
              {
                method: "POST",
                headers: {
                  Authorization: "Bearer " + this.config.openAIKey,
                  "Content-Type": "application/json",
                  "X-Client-Request-Id": `${id}-${index}`,
                },
                body: JSON.stringify(
                  imageRequest(this.config, imageUrl, session.style),
                ),
                signal: AbortSignal.timeout(45000),
              },
            );
            if (!response.ok) {
              await response.body?.cancel();
              throw new ApiError(502, "generation_start_failed");
            }
            const result = await response.json() as ProviderResponse;
            if (!result.id?.startsWith("resp_")) {
              throw new ApiError(502, "invalid_provider_response");
            }
            await this.updateJob(id, index, {
              state: "running",
              response_id: result.id,
            });
            return reply({ state: "running" }, 202);
          } catch {
            await this.updateJob(id, index, { state: "uncertain" });
            throw new ApiError(502, "generation_uncertain_no_auto_retry");
          }
        }
        if (request.method === "GET") {
          const job = await this.job(id, index);
          if (!job) return reply({ state: "queued" });
          if (job.state === "ready" && job.object_key) {
            return reply({
              state: "ready",
              url: await this.signed(job.object_key),
            });
          }
          if (job.state !== "running" || !job.response_id) {
            return reply({ state: job.state });
          }
          const response = await this.send(
            "https://api.openai.com/v1/responses/" +
              encodeURIComponent(job.response_id),
            {
              headers: { Authorization: "Bearer " + this.config.openAIKey },
              signal: AbortSignal.timeout(45000),
            },
          );
          if (!response.ok) {
            await response.body?.cancel();
            throw new ApiError(502, "generation_status_unavailable");
          }
          const result = await response.json() as ProviderResponse;
          if (["queued", "in_progress"].includes(result.status)) {
            return reply({ state: "running" });
          }
          const images = result.output?.filter((o) =>
            o.type === "image_generation_call" && o.result
          ) ?? [];
          if (result.status !== "completed" || images.length !== 1) {
            await this.updateJob(id, index, { state: "failed" });
            return reply({ state: "failed" });
          }
          const bytes = new Uint8Array(
            Buffer.from(images[0].result!, "base64"),
          );
          jpeg(bytes);
          if (bytes.length > 10 * 1024 * 1024) {
            throw new ApiError(502, "generated_image_too_large");
          }
          const key = `${id}/generated-${index}.jpg`;
          await this.upload(key, bytes, "image/jpeg");
          await this.updateJob(id, index, {
            state: "ready",
            object_key: key,
            usage: { response: result.usage, image: images[0].usage },
          });
          return reply({ state: "ready", url: await this.signed(key) });
        }
      }
      if (match[4] !== undefined && request.method === "PUT") {
        const name = match[4];
        if (
          !/^((photo|original)-[1-4]\.jpg|sheet\.jpg|archive\.zip)$/.test(
            name,
          ) ||
          ((name.startsWith("photo-") || name.startsWith("original-")) &&
            Number(name.split("-")[1][0]) > session.photo_count)
        ) throw new ApiError(400, "invalid_asset");
        const isZip = name === "archive.zip";
        const bytes = await readBytes(
          request,
          isZip ? 50 * 1024 * 1024 : 10 * 1024 * 1024,
        );
        if (isZip) {
          if (
            bytes[0] !== 80 || bytes[1] !== 75 || bytes[2] !== 3 ||
            bytes[3] !== 4
          ) throw new ApiError(400, "invalid_archive");
        } else jpeg(bytes);
        const digest = await hash(bytes);
        const existing = await this.rest<Asset[]>(
          `booth_assets?session_id=eq.${id}&name=eq.${name}&select=*`,
        );
        if (existing.length && existing[0].sha256 !== digest) {
          throw new ApiError(409, "asset_conflict");
        }
        if (existing[0]?.uploaded) return reply({ sha256: digest });
        if (session.published_at) throw new ApiError(409, "already_published");
        const key = `${id}/${name}`;
        await this.rest("booth_assets", "POST", {
          session_id: id,
          name,
          object_key: key,
          sha256: digest,
          bytes: bytes.length,
        }, "resolution=ignore-duplicates,return=representation");
        const reserved = await this.rest<Asset[]>(
          `booth_assets?session_id=eq.${id}&name=eq.${name}&select=*`,
        );
        if (reserved[0]?.sha256 !== digest) {
          throw new ApiError(409, "asset_conflict");
        }
        if (!reserved[0].uploaded) {
          await this.upload(
            key,
            bytes,
            isZip ? "application/zip" : "image/jpeg",
          );
          await this.rest(
            `booth_assets?session_id=eq.${id}&name=eq.${name}`,
            "PATCH",
            { uploaded: true },
          );
        }
        return reply({ sha256: digest }, 201);
      }
      if (match[2] === "share" && request.method === "POST") {
        const { token, used_original_fallback } = await jsonBody(request);
        if (typeof token !== "string" || !/^[a-f0-9]{64}$/.test(token)) {
          throw new ApiError(400, "invalid_share_token");
        }
        const assets = await this.rest<Asset[]>(
          `booth_assets?session_id=eq.${id}&uploaded=eq.true&select=*`,
        );
        const expected = [
          "sheet.jpg",
          "archive.zip",
          ...Array.from(
            { length: session.photo_count },
            (_, i) => `photo-${i + 1}.jpg`,
          ),
        ];
        if (session.mode === "surprise") {
          expected.push(
            ...Array.from(
              { length: session.photo_count },
              (_, i) => `original-${i + 1}.jpg`,
            ),
          );
        }
        if (expected.some((name) => !assets.some((a) => a.name === name))) {
          throw new ApiError(409, "missing_assets");
        }
        const digest = await hash(token);
        if (session.share_hash && session.share_hash !== digest) {
          throw new ApiError(409, "share_conflict");
        }
        await this.rest(`booth_sessions?id=eq.${id}`, "PATCH", {
          share_hash: digest,
          used_original_fallback: used_original_fallback === true,
          published_at: session.published_at ?? new Date().toISOString(),
        });
        const remaining = Math.max(
          1,
          Math.min(
            604800,
            Math.floor((Date.parse(session.expires_at) - Date.now()) / 1000),
          ),
        );
        return reply({
          token,
          expires_at: session.expires_at,
          download_url: await this.signed(`${id}/archive.zip`, remaining),
        });
      }
      throw new ApiError(405, "method_not_allowed");
    } catch (error) {
      return reply({
        code: error instanceof ApiError ? error.code : "temporary_failure",
        retryable: false,
      }, error instanceof ApiError ? error.status : 503);
    }
  }
}
function uuid(value: string) {
  return /^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$/.test(
    value,
  );
}
