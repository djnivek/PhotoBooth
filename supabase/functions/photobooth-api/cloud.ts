import { Buffer } from "node:buffer";
import { identityPrompt, requestedStyles, freezePlan, styleId, type FrozenShotStyle } from "./style-plans.ts";
import catalog from "./styles.json" with { type: "json" };
import { buildArchive } from "./archive.ts";
import { boothMetrics, guestMetrics } from "./monitoring.ts";
import { viewerRoute, ViewerError } from "./viewer.ts";

export type Config = {
  url: string;
  serviceKey: string;
  accessKeyHash: string;
  openAIKey: string;
  galleryOrigin: string;
  imageModel: string;
};
export type SessionRow = {
  id: string;
  owner_hash: string;
  photo_count: number;
  mode: string;
  style: string;
  requested_style?: string;
  style_prompt?: string;
  requested_styles?: string[] | null;
  shot_styles?: FrozenShotStyle[] | null;
  share_hash: string | null;
  published_at: string | null;
  used_original_fallback?: boolean;
  expires_at: string;
  created_at?: string;
};
type Job = {
  state: string;
  response_id?: string;
  object_key?: string;
  updated_at?: string;
  usage?: Record<string, unknown>;
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
  service_tier?: string;
};

export class ApiError extends Error {
  constructor(public status: number, public code: string) {
    super(code);
  }
}
// Legacy definitions preserve already created sessions across a catalog update.
const legacyStyles: Record<string, string> = {
  cinema: "Un portrait de cinéma d’aventure élégant dans un décor de première de film.",
  garden: "Un jardin enchanté de fête avec fleurs lumineuses et ambiance de conte photographique.",
  painting: "Un décor de gouache expressive entourant des personnes aux visages photographiques fidèles.",
};
export const styles: Record<string, string> = {
  ...legacyStyles, ...Object.fromEntries(catalog.styles.map(s => [s.id, s.prompt])),
};
export { identityPrompt } from "./style-plans.ts";
export function publicCatalog() {
  return { version: catalog.version, styles: catalog.styles.map(({ prompt: _prompt, ...style }) => style) };
}
export function imageRequest(config: Config, imageUrl: string, style: string, savedPrompt?: string, frozenPrompt?: string) {
  if (!frozenPrompt && !savedPrompt && !(style in styles)) throw new ApiError(400, "invalid_style");
  return {
    model: config.imageModel,
    images: [{ image_url: imageUrl }],
    prompt: frozenPrompt ?? ("Transforme cette seule photo pour un anniversaire familial. " + identityPrompt +
      " Univers choisi : " + (savedPrompt ?? styles[style])),
    n: 1,
    quality: "medium",
    size: "1024x1024",
    output_format: "jpeg",
    output_compression: 95,
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
    private readonly background?: (task: Promise<void>) => void,
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
    if (!response.ok) {
      // Only allowlisted claim errors are returned. Never expose SQL or credentials.
      if (path === "rpc/booth_claim_job") {
        const error = await response.json().catch(() => ({}));
        const limits: Record<string, number> = {
          generation_disabled: 503, event_quota_reached: 429,
          generation_rate_limited: 429, generation_busy: 429, already_published: 409,
        };
        if (error.code === "P0001" && Object.hasOwn(limits, error.message)) {
          throw new ApiError(limits[error.message], error.message);
        }
      }
      throw new ApiError(503, "database_unavailable");
    }
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
  private frozenShot(session: SessionRow, index: number): FrozenShotStyle | undefined {
    if (session.shot_styles == null) return undefined; // pre-migration session
    if (!Array.isArray(session.shot_styles) || session.shot_styles.length !== session.photo_count ||
        session.shot_styles.some(shot => !shot || !styleId(shot.id) || shot.id === "chef" ||
          typeof shot.prompt !== "string" || !shot.prompt.trim() || typeof shot.variation !== "string" || !shot.variation.trim()))
      throw new ApiError(503, "invalid_session_plan");
    return session.shot_styles[index];
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

  private async saveAsset(session: SessionRow, name: string, bytes: Uint8Array, contentType: string) {
    const id = session.id; const digest = await hash(bytes);
    const path = `booth_assets?session_id=eq.${id}&name=eq.${name}`;
    const existing = (await this.rest<Asset[]>(path + "&select=*"))[0];
    if (existing && existing.sha256 !== digest) throw new ApiError(409, "asset_conflict");
    if (existing?.uploaded) return existing;
    if (session.published_at) throw new ApiError(409, "already_published");
    const asset = { session_id: id, name, object_key: `${id}/${name}`, sha256: digest, bytes: bytes.length };
    await this.rest("booth_assets", "POST", asset, "resolution=ignore-duplicates,return=representation");
    const reserved = (await this.rest<Asset[]>(path + "&select=*"))[0];
    if (reserved?.sha256 !== digest) throw new ApiError(409, "asset_conflict");
    if (!reserved.uploaded) {
      await this.upload(asset.object_key, bytes, contentType);
      await this.rest(path, "PATCH", { uploaded: true });
    }
    return { ...asset, uploaded: true };
  }

  private async archiveSession(session: SessionRow, assets: Asset[], fallback: boolean) {
    const entries = Array.from({ length: session.photo_count }, (_, i) => ({
      source: `${fallback ? "original" : "photo"}-${i + 1}.jpg`, name: `photo-${i + 1}.jpg`,
    }));
    if (session.mode === "surprise") entries.push(...Array.from({ length: session.photo_count }, (_, i) => ({ source: `original-${i + 1}.jpg`, name: `originaux/photo-${i + 1}.jpg` })));
    entries.push({ source: "sheet.jpg", name: "planche-4x6.jpg" });
    const total = entries.reduce((sum, entry) => sum + (assets.find(a => a.name === entry.source)?.bytes ?? 0), 0);
    if (total > 50 * 1024 * 1024 - 4096) throw new ApiError(413, "archive_too_large");
    const contents = await Promise.all(entries.map(async entry => {
      const asset = assets.find(a => a.name === entry.source);
      if (!asset) throw new ApiError(409, "missing_assets");
      const response = await this.send(`${this.config.url}/storage/v1/object/authenticated/booth-private/${asset.object_key}`, {
        headers: { apikey: this.config.serviceKey, Authorization: "Bearer " + this.config.serviceKey }, signal: AbortSignal.timeout(30000),
      });
      if (!response.ok) { await response.body?.cancel(); throw new ApiError(503, "storage_unavailable"); }
      const bytes = await readBytes(new Request("https://internal.invalid", { method: "POST", body: response.body }), 10 * 1024 * 1024);
      if (bytes.length !== asset.bytes || await hash(bytes) !== asset.sha256) throw new ApiError(503, "asset_integrity_failed");
      return { name: entry.name, bytes };
    }));
    return await this.saveAsset(session, "archive.zip", buildArchive(contents), "application/zip");
  }

  private async recordMetrics(events: unknown[], required = false) {
    try {
      const response = await this.send(this.config.url + "/rest/v1/rpc/booth_metrics_ingest", {
        method: "POST", headers: { apikey: this.config.serviceKey, Authorization: "Bearer " + this.config.serviceKey, "Content-Type": "application/json" },
        body: JSON.stringify({ p_events: events }), signal: AbortSignal.timeout(5000),
      });
      if (!response.ok) throw new Error("metrics_unavailable");
      await response.body?.cancel();
    } catch { if (required) throw new ApiError(503, "metrics_unavailable"); }
  }
  private async generationMetric(session: SessionRow, index: number, started: number, duration: number, outcome: string, bytes?: number) {
    const task = this.recordMetrics([{ id: crypto.randomUUID(), session_id: session.id, origin: "provider", kind: "generation",
      occurred_at: new Date(started).toISOString(), mode: session.mode, photo_count: session.photo_count,
      requested_style: session.requested_style ?? "chef", style: session.shot_styles?.[index]?.id ?? session.style, source: "unknown", shot_index: index,
      duration_ms: duration, bytes: bytes ?? null, outcome, asset: null, view_id: null }]);
    if (this.background) this.background(task); else await task;
  }

  // One claimed edit, executed by the Edge runtime after the 202 response.
  // Images API has no recoverable Responses ID: never repeat an ambiguous call.
  private async editImage(session: SessionRow, index: number, imageUrl: string,
    inputBytes: number, direct: boolean, started: number) {
    const usage = {
      provider: "images", request: { image_model: this.config.imageModel, quality: "medium", size: "1024x1024" },
      input_bytes: inputBytes, started_at: new Date(started).toISOString(),
    };
    const providerStarted = Date.now();
    let providerElapsed: number | undefined;
    try {
      const response = await this.send("https://api.openai.com/v1/images/edits", {
        method: "POST",
        headers: { Authorization: "Bearer " + this.config.openAIKey, "Content-Type": "application/json",
          "X-Client-Request-Id": `${session.id}-${index}` },
        body: JSON.stringify(imageRequest(this.config, imageUrl, this.frozenShot(session, index)?.id ?? session.style, session.style_prompt, this.frozenShot(session, index)?.prompt)),
        // Leave room for Storage/DB finalization within a Free Edge worker's lifetime.
        signal: AbortSignal.timeout(110000),
      });
      if (!response.ok) {
        await response.body?.cancel();
        await this.generationMetric(session,index,started,Date.now()-providerStarted,response.status >= 400 && response.status < 500 && response.status !== 408 ? "failed" : "uncertain");
        const rejected = response.status >= 400 && response.status < 500 && response.status !== 408;
        await this.updateJob(session.id, index, { state: rejected ? "failed" : "uncertain",
          usage: { ...usage, status: response.status, error_code: response.status === 429 ? "provider_rate_limited" : "generation_not_completed" } });
        return;
      }
      const encoded = await readBytes(new Request("https://internal.invalid", { method: "POST", body: response.body }), 15 * 1024 * 1024);
      providerElapsed = Date.now()-providerStarted;
      const result = JSON.parse(new TextDecoder().decode(encoded));
      if (result.data?.length !== 1 || typeof result.data[0].b64_json !== "string") throw new Error("Invalid image response");
      const bytes = new Uint8Array(Buffer.from(result.data[0].b64_json, "base64"));
      jpeg(bytes);
      if (bytes.length > 10 * 1024 * 1024) throw new Error("Image too large");
      await this.generationMetric(session,index,started,providerElapsed,"success",bytes.length);
      const asset = direct ? await this.saveAsset(session, `photo-${index + 1}.jpg`, bytes, "image/jpeg") : null;
      const objectKey = asset?.object_key ?? `${session.id}/generated-${index}.jpg`;
      if (!direct) await this.upload(objectKey, bytes, "image/jpeg");
      await this.updateJob(session.id, index, { state: "ready", object_key: objectKey,
        usage: { ...usage, image: result.usage, provider_elapsed_ms: providerElapsed, processing_elapsed_ms: Date.now() - started, result_bytes: bytes.length, direct_storage: direct } });
    } catch {
      await this.generationMetric(session,index,started,providerElapsed ?? Date.now()-providerStarted,"uncertain");
      // A network loss or runtime timeout never authorizes another paid edit.
      await this.updateJob(session.id, index, { state: "uncertain", usage: { ...usage, error_code: "generation_uncertain_no_auto_retry" } })
        .catch(() => { /* Stale-job polling handles a failed final DB write. */ });
    }
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
          api_version: "0.9.0",
          stage: "rehearsal",
          production_ready: false,
          session_api_enabled: !!this.config.accessKeyHash,
          image_generation_enabled: !!this.config.openAIKey &&
            !!this.config.accessKeyHash,
        });
      }
      if (path === "/styles" && request.method === "GET") return reply(publicCatalog());
      if (["/downloads/resolve", "/downloads/events"].includes(path) && request.method === "POST") {
        const guestBody = await jsonBody(request);
        const { token } = guestBody;
        if (typeof token !== "string" || !/^[a-f0-9]{64}$/.test(token)) {
          throw new ApiError(404, "link_unavailable");
        }
        const rows = await this.rest<SessionRow[]>(
          `booth_sessions?share_hash=eq.${await hash(token)}&select=*`,
        );
        const row = rows[0];
        if (
          !row || Date.parse(row.expires_at) <= Date.now()
        ) throw new ApiError(404, "link_unavailable");
        const assets = await this.rest<Asset[]>(
          `booth_assets?session_id=eq.${row.id}&uploaded=eq.true&select=*`,
        );
        // Guests only read already uploaded assets. Resolving a QR never starts,
        // polls or retries a provider request, and never exposes a response ID.
        const jobs = row.published_at ? [] : await this.rest<{ state: string }[]>(
          `booth_jobs?session_id=eq.${row.id}&select=state`,
        );
        const failed = jobs.some(j => ["failed", "uncertain"].includes(j.state));
        const stalled = !!row.created_at && Date.now() - Date.parse(row.created_at) > 10 * 60 * 1000;
        const publicAssets = assets.filter(a => /^(photo|original)-[1-4]\.jpg$|^(sheet\.jpg|archive\.zip)$/.test(a.name));
        const visible = row.used_original_fallback ? publicAssets.filter(a => !/^photo-[1-4]\.jpg$/.test(a.name)) : row.published_at ? publicAssets : publicAssets.filter(a => /^(photo|original)-[1-4]\.jpg$/.test(a.name));
        if (path === "/downloads/events") {
          let events;
          try { events = guestMetrics(guestBody,row,visible.map(a=>a.name)); } catch { throw new ApiError(400,"invalid_metrics"); }
          await this.recordMetrics(events,true);
          return reply({ accepted: events.length });
        }
        return reply({
          state: row.published_at ? "ready" : failed ? "failed" : stalled ? "delayed" : "processing",
          style: row.style,
          completed_count: assets.filter(a => /^photo-[1-4]\.jpg$/.test(a.name)).length,
          expires_at: row.expires_at,
          photo_count: row.photo_count,
          mode: row.mode,
          used_original_fallback: row.used_original_fallback === true,
          assets: await Promise.all(
            visible.map(async (a) => ({
              name: a.name,
              url: await this.signed(a.object_key),
            })),
          ),
        });
      }
      const bearer =
        request.headers.get("authorization")?.replace(/^Bearer /, "") ?? "";
      if (['/viewer/feed','/viewer/photos'].includes(path) && request.method==='POST') {
        try { return reply(await viewerRoute(path,await jsonBody(request),null,{rest:this.rest.bind(this),hash,signed:this.signed.bind(this)})); }
        catch(e) { if(e instanceof ViewerError) throw new ApiError(e.status,e.code); throw e; }
      }
      if (
        !bearer || bearer.length > 256 || !equalHash(await hash(bearer), this.config.accessKeyHash)
      ) throw new ApiError(401, "unauthorized");
      const owner = this.config.accessKeyHash;
      if ((path==='/viewer/settings' || /^\/sessions\/[a-f0-9-]+\/viewer$/i.test(path)) && request.method==='POST') {
        try { return reply(await viewerRoute(path,await jsonBody(request),owner,{rest:this.rest.bind(this),hash,signed:this.signed.bind(this)})); }
        catch(e) { if(e instanceof ViewerError) throw new ApiError(e.status,e.code); throw e; }
      }
      if (path === "/monitoring/booth" && request.method === "POST") {
        let events;
        try { events = boothMetrics(await jsonBody(request)); } catch { throw new ApiError(400,"invalid_metrics"); }
        await this.recordMetrics(events,true);
        return reply({ accepted: events.length });
      }
      if (path === "/booth/check" && request.method === "GET") {
        return reply({
          authenticated: true,
          image_generation_enabled: !!this.config.openAIKey,
          image_model: this.config.imageModel,
          generation_api: "images/edits",
          image_quality: "medium",
          image_size: "1024x1024",
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
        let choices: string[];
        try { choices = requestedStyles(input, input.photo_count); }
        catch { throw new ApiError(400, "invalid_style"); }
        const requestedStyle = choices.length === 1 ? choices[0] : "mix";
        // A prior session wins, including its exact plan and removed catalog themes.
        const existing = await this.rest<SessionRow[]>(`booth_sessions?id=eq.${input.id}&owner_hash=eq.${owner}&select=*`);
        if (!existing.length) {
          let plan: FrozenShotStyle[] | null = null;
          try { if (input.mode === "surprise") plan = freezePlan(choices, input.photo_count); }
          catch { throw new ApiError(400, "invalid_style"); }
          const single = plan && plan.every(s => s.id === plan[0].id) ? plan[0].id : "mix";
          await this.rest("booth_sessions", "POST", {
            id: input.id, owner_hash: owner, photo_count: input.photo_count, mode: input.mode,
            style: input.mode === "original" ? "original" : single,
            requested_style: requestedStyle, requested_styles: choices,
            style_prompt: plan ? styles[plan[0].id] : null, shot_styles: plan,
          }, "resolution=ignore-duplicates,return=representation");
        }
        const session = await this.session(input.id, owner);
        const savedChoices = session.requested_styles ?? [session.requested_style ?? "chef"];
        if (session.photo_count !== input.photo_count || session.mode !== input.mode ||
            JSON.stringify([...savedChoices].sort()) !== JSON.stringify(choices)) throw new ApiError(409, "session_conflict");
        return reply({
          id: session.id,
          style: session.style,
          shot_styles: session.mode === "surprise" ? Array.from({length: session.photo_count}, (_, index) => this.frozenShot(session, index)?.id ?? session.style) : [],
          expires_at: session.expires_at,
        }, 201);
      }
      const match = path.match(
        /^\/sessions\/([a-f0-9-]{36})\/(photos\/([0-3])|assets\/([^/]+)|share|gallery)$/,
      );
      if (!match || !uuid(match[1])) throw new ApiError(404, "not_found");
      const id = match[1];
      const session = await this.session(id, owner);
      if (match[2] === "gallery" && request.method === "POST") {
        const { token } = await jsonBody(request);
        if (typeof token !== "string" || !/^[a-f0-9]{64}$/.test(token)) throw new ApiError(400, "invalid_share_token");
        const digest = await hash(token);
        if (session.share_hash && session.share_hash !== digest) throw new ApiError(409, "share_conflict");
        // Conditional update prevents concurrent clients replacing a capability.
        await this.rest(`booth_sessions?id=eq.${id}&or=(share_hash.is.null,share_hash.eq.${digest})`, "PATCH", { share_hash: digest });
        if ((await this.session(id, owner)).share_hash !== digest) throw new ApiError(409, "share_conflict");
        return reply({
          gallery_url: this.config.galleryOrigin.replace(/\/$/, "") + "/#" + token,
          expires_at: session.expires_at,
        });
      }
      if (match[3] !== undefined) {
        const index = Number(match[3]);
        if (index >= session.photo_count || session.mode !== "surprise") {
          throw new ApiError(400, "invalid_shot");
        }
        this.frozenShot(session, index); // reject a damaged plan before claiming any quota
        if (request.method === "POST") {
          if (!this.config.openAIKey) {
            throw new ApiError(503, "openai_not_configured");
          }
          await jsonBody(request);
          let originals = await this.rest<Asset[]>(
            `booth_assets?session_id=eq.${id}&name=eq.input-${index + 1}.jpg&uploaded=eq.true&select=*`,
          );
          if (!originals.length) originals = await this.rest<Asset[]>(
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
          const started = Date.now();
          await this.updateJob(id, index, { state: "running", usage: { provider: "images", started_at: new Date(started).toISOString() } });
          const direct = originals[0].name.startsWith("input-") || new URL(request.url).searchParams.get("transport") === "direct";
          const task = this.editImage(session, index, imageUrl, originals[0].bytes, direct, started);
          if (this.background) this.background(task);
          else await task; // deterministic test/local host without an Edge lifetime hook
          return reply({ state: "running" }, 202);
        }
        if (request.method === "GET") {
          const job = await this.job(id, index);
          if (!job) return reply({ state: "queued" });
          if (job.usage?.provider === "images" && job.state !== "ready") {
            // Recover a stored result if the worker stopped before its final DB update.
            const asset = (await this.rest<Asset[]>(`booth_assets?session_id=eq.${id}&name=eq.photo-${index + 1}.jpg&uploaded=eq.true&select=*`))[0];
            if (asset) {
              await this.updateJob(id, index, { state: "ready", object_key: asset.object_key });
              return reply({ state: "ready", url: await this.signed(asset.object_key), asset_stored: true, sha256: asset.sha256 });
            }
            if (job.state === "running" && Date.now() - Date.parse(job.updated_at ?? "") > 180000) {
              // Compare the timestamp as well as state: never overwrite a concurrent completion.
              await this.rest(`booth_jobs?session_id=eq.${id}&shot_index=eq.${index}&state=eq.running&updated_at=eq.${encodeURIComponent(job.updated_at!)}`,
                "PATCH", { state: "uncertain", updated_at: new Date().toISOString() });
              return reply({ state: (await this.job(id, index))?.state ?? "uncertain" });
            }
          }
          if (job.state === "ready" && job.object_key) {
            const asset = (await this.rest<Asset[]>(`booth_assets?session_id=eq.${id}&name=eq.photo-${index + 1}.jpg&uploaded=eq.true&select=*`))[0];
            return reply({
              state: "ready",
              url: await this.signed(asset?.object_key ?? job.object_key),
              asset_stored: !!asset,
              sha256: asset?.sha256,
            });
          }
          if (job.state !== "running" || !job.response_id) {
            return reply({ state: job.state, code: job.usage?.error_code });
          }
          // Only jobs created before 0.6 have a Responses ID. Finish them without
          // starting a new Responses request; all new edits use Images API above.
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
          const direct = new URL(request.url).searchParams.get("transport") === "direct";
          const asset = direct ? await this.saveAsset(session, `photo-${index + 1}.jpg`, bytes, "image/jpeg") : null;
          const key = asset?.object_key ?? `${id}/generated-${index}.jpg`;
          if (!direct) await this.upload(key, bytes, "image/jpeg");
          await this.updateJob(id, index, {
            state: "ready",
            object_key: key,
            usage: { ...job.usage, response: result.usage, image: images[0].usage, served_service_tier: result.service_tier ?? null,
              processing_elapsed_ms: Math.max(0, Date.now() - Date.parse(job.updated_at ?? new Date().toISOString())), result_bytes: bytes.length, direct_storage: direct },
          });
          return reply({ state: "ready", url: await this.signed(key), asset_stored: !!asset, sha256: asset?.sha256 });
        }
      }
      if (match[4] !== undefined && request.method === "PUT") {
        const name = match[4];
        if (
          !/^((photo|original|input)-[1-4]\.jpg|sheet\.jpg|archive\.zip)$/.test(
            name,
          ) ||
          ((name.startsWith("photo-") || name.startsWith("original-") || name.startsWith("input-")) &&
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
        const asset = await this.saveAsset(session, name, bytes, isZip ? "application/zip" : "image/jpeg");
        return reply({ sha256: asset.sha256 }, 201);
      }
      if (match[2] === "share" && request.method === "POST") {
        const { token, used_original_fallback } = await jsonBody(request);
        if (used_original_fallback === true && session.mode !== "surprise") throw new ApiError(400, "invalid_fallback");
        if (typeof token !== "string" || !/^[a-f0-9]{64}$/.test(token)) {
          throw new ApiError(400, "invalid_share_token");
        }
        const assets = await this.rest<Asset[]>(
          `booth_assets?session_id=eq.${id}&uploaded=eq.true&select=*`,
        );
        const expected = [
          "sheet.jpg",
          ...Array.from(
            { length: used_original_fallback === true ? 0 : session.photo_count },
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
        if (!assets.some(a => a.name === "archive.zip")) await this.archiveSession(session, assets, used_original_fallback === true);
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
          gallery_url: this.config.galleryOrigin.replace(/\/$/, "") + "/#" + token,
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
