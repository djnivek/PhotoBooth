export const metricAsset = /^(input-[1-4]\.jpg|original-[1-4]\.jpg|photo-[1-4]\.jpg|sheet\.jpg|archive\.zip)$/;
const uuid = (v: unknown) => typeof v === "string" && /^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$/i.test(v);
const style = (v: unknown) => typeof v === "string" && /^[a-z][a-z0-9_-]{0,47}$/.test(v);
export function boothMetrics(body: Record<string, unknown>) {
  if (!Array.isArray(body.events) || body.events.length < 1 || body.events.length > 10) throw new Error("invalid_metrics");
  return body.events.map(e => {
    if (!e || !uuid(e.id) || !uuid(e.session_id) || !["session_started","capture_saved","session_ready","session_failed","session_cancelled","original_fallback","upload","download"].includes(e.kind) ||
      !["original","surprise"].includes(e.mode) || ![1,4].includes(e.photo_count) || !["camera","fixture"].includes(e.source) || !style(e.requested_style) ||
      (e.style != null && !style(e.style)) || !Number.isInteger(e.shot_index) || e.shot_index < -1 || e.shot_index >= e.photo_count ||
      !Number.isFinite(Date.parse(e.occurred_at)) || Date.parse(e.occurred_at) > Date.now()+86400000 || Date.parse(e.occurred_at) < Date.now()-366*86400000) throw new Error("invalid_metrics");
    const transfer = ["upload","download"].includes(e.kind);
    if (transfer && (!Number.isInteger(e.duration_ms) || e.duration_ms < 0 || e.duration_ms > 900000 || !Number.isInteger(e.bytes) || e.bytes < 0 || e.bytes > 50*1024*1024 || !metricAsset.test(e.asset) || !["success","failed"].includes(e.outcome))) throw new Error("invalid_metrics");
    if(e.kind === "capture_saved" && e.shot_index < 0) throw new Error("invalid_metrics");
    return { id:e.id,session_id:e.session_id,kind:e.kind,origin:"booth",occurred_at:new Date(e.occurred_at).toISOString(),mode:e.mode,photo_count:e.photo_count,
      requested_style:e.requested_style,style:e.style??null,source:e.source,shot_index:e.shot_index,duration_ms:transfer?e.duration_ms:null,bytes:transfer?e.bytes:null,
      asset:transfer?e.asset:null,outcome:transfer?e.outcome:null,view_id:null };
  });
}
export function guestMetrics(body: Record<string, unknown>, session: {id:string;mode:string;photo_count:number;style:string;requested_style?:string}, visible: string[]) {
  if (!Array.isArray(body.events) || body.events.length < 1 || body.events.length > 10) throw new Error("invalid_metrics");
  return body.events.map(e => {
    if(!e || !uuid(e.id) || !uuid(e.view_id) || !["gallery_view","image_view","download_requested"].includes(e.kind) ||
      (e.kind !== "gallery_view" && (!visible.includes(e.asset) || e.asset.startsWith("input-") || (e.kind === "image_view" && !e.asset.endsWith(".jpg"))))) throw new Error("invalid_metrics");
    return { id:e.id,session_id:session.id,origin:"guest",kind:e.kind,view_id:e.view_id,asset:e.kind === "gallery_view"?null:e.asset,
      occurred_at:new Date().toISOString(),mode:session.mode,photo_count:session.photo_count,requested_style:session.requested_style??"chef",style:session.style,
      source:"unknown",shot_index:-1,duration_ms:null,bytes:null,outcome:null };
  });
}
