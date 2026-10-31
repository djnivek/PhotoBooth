type Row = { id: string; photo_count: number; mode: string; style: string; released_at: string; photos: { name: string; key: string }[] };
type Access = { owner_hash: string; enabled: boolean };
export type ViewerDependencies = {
  rest: <T>(path: string, method?: string, body?: unknown, prefer?: string) => Promise<T>;
  hash: (value: string) => Promise<string>;
  signed: (key: string) => Promise<string>;
};
export class ViewerError extends Error { constructor(public status: number, public code: string) { super(code); } }
const secret = (v: unknown): v is string => typeof v === 'string' && /^[a-f0-9]{64}$/.test(v);
const uuid = (v: unknown): v is string => typeof v === 'string' && /^[a-f0-9]{8}-[a-f0-9]{4}-[1-5][a-f0-9]{3}-[89ab][a-f0-9]{3}-[a-f0-9]{12}$/i.test(v);

export async function viewerRoute(path: string, body: Record<string, unknown>, owner: string | null, d: ViewerDependencies): Promise<unknown> {
  if (path === '/viewer/settings') {
    if (!owner) throw new ViewerError(401,'unauthorized');
    if (!secret(body.token) || typeof body.enabled !== 'boolean' || typeof body.include_tests !== 'boolean') throw new ViewerError(400,'invalid_viewer');
    await d.rest('booth_viewers?on_conflict=owner_hash','POST', {owner_hash:owner,token_hash:await d.hash(body.token),enabled:body.enabled,include_tests:body.include_tests,updated_at:new Date().toISOString()},'resolution=merge-duplicates,return=minimal');
    return {accepted:true};
  }
  const match=path.match(/^\/sessions\/([a-f0-9-]+)\/viewer$/i);
  if (match) {
    if (!owner) throw new ViewerError(401,'unauthorized');
    if (!uuid(match[1]) || !['camera','fixture'].includes(String(body.source))) throw new ViewerError(400,'invalid_viewer');
    const sessions=await d.rest<{id:string}[]>(`booth_sessions?id=eq.${match[1]}&owner_hash=eq.${owner}&select=id`);
    if (!sessions.length) throw new ViewerError(404,'session_unavailable');
    const enabled=await d.rest<Access[]>(`booth_viewers?owner_hash=eq.${owner}&enabled=eq.true&select=owner_hash,enabled`);
    if (!enabled.length) return {accepted:true,scheduled:false};
    await d.rest('booth_projections?on_conflict=session_id','POST',{session_id:match[1],source:body.source},'resolution=ignore-duplicates,return=minimal');
    return {accepted:true,scheduled:true};
  }
  if (!['/viewer/feed','/viewer/photos'].includes(path)) throw new ViewerError(404,'not_found');
  if (!secret(body.token)) throw new ViewerError(404,'viewer_unavailable');
  const viewers=await d.rest<Access[]>(`booth_viewers?token_hash=eq.${await d.hash(body.token)}&select=owner_hash,enabled`);
  if (!viewers.length) throw new ViewerError(404,'viewer_unavailable');
  if (!viewers[0].enabled) return {enabled:false,sessions:[]};
  const rows=await d.rest<Row[]>('rpc/booth_viewer_feed','POST',{p_owner:viewers[0].owner_hash});
  if (path === '/viewer/feed') {
    const sessions=rows.map(({photos,...r})=>({...r,photos:photos.map(p=>({name:p.name}))}));
    const revision=await d.hash(JSON.stringify(sessions));
    return body.known===revision?{enabled:true,unchanged:true,revision}:{enabled:true,sessions,revision};
  }
  if (!uuid(body.session_id)) throw new ViewerError(400,'invalid_session');
  const row=rows.find(s=>s.id===body.session_id);
  if (!row) throw new ViewerError(404,'session_unavailable');
  return {enabled:true,id:row.id,photos:await Promise.all(row.photos.map(async p=>({name:p.name,url:await d.signed(p.key)})))};
}
