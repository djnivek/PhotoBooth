// Local-only administration API. No frontend/provider/service-role key is distributed.
const http=require('node:http');
const fs=require('node:fs/promises');
const path=require('node:path');
const os=require('node:os');
const crypto=require('node:crypto');
const {execFile}=require('node:child_process');
const {promisify}=require('node:util');
const run=promisify(execFile);
const project='frkckrvsdsiwivdfyjau';
function equal(a,b){const x=Buffer.from(a||'');const y=Buffer.from(b);return x.length===y.length&&crypto.timingSafeEqual(x,y);}
async function readReport(days,fixtures,{cli=process.env.PHOTOBOOTH_SUPABASE_CLI||'supabase',cwd=path.resolve(__dirname,'../..'),execute=run}={}){
 // Current CLI selects the project from the linked checkout, not a query flag.
 if(!Number.isInteger(days)||days<1||days>366||typeof fixtures!=='boolean')throw new Error('Invalid report range');
 const linkedProject=(await fs.readFile(path.join(cwd,'supabase/.temp/project-ref'),'utf8')).trim();
 if(linkedProject!==project)throw new Error('Link this checkout to the PhotoBooth project before reading reports');
 const directory=await fs.mkdtemp(path.join(os.tmpdir(),'photobooth-admin-'));
 try{
  const file=path.join(directory,'report.sql');
  // Interpolated values have already been reduced to a bounded integer and boolean.
  await fs.writeFile(file,'select public.booth_monitoring_report('+days+','+(fixtures?'true':'false')+') as report;');
  const {stdout}=await execute(cli,['db','query','--linked','--file',file,'--output','json'],{cwd,windowsHide:true,timeout:25000,maxBuffer:4*1024*1024});
  const output=JSON.parse(stdout);const rows=Array.isArray(output)?output:output?.rows;
  const result=rows?.[0]?.report;
  if(!result||!result.totals||!Array.isArray(result.timings))throw new Error('Invalid report response');
  return result;
 }finally{
  // Delete only the exact two paths we created; no recursive shell deletion.
  await fs.unlink(path.join(directory,'report.sql')).catch(()=>{});await fs.rmdir(directory).catch(()=>{});
 }
}
function createAdminServer({token,read=readReport,tailnetHost}={}){
 if(typeof token!=='string'||token.length<32)throw new Error('Strong local token required');
 if(tailnetHost&&!/^[a-z0-9-]+\.[a-z0-9.-]+\.ts\.net$/.test(tailnetHost))throw new Error('Use the exact Tailscale .ts.net hostname');
 const browserCredential='Basic '+Buffer.from('admin:'+token).toString('base64');
 const cache=new Map();const inFlight=new Map();
 const server=http.createServer(async(req,res)=>{
  const respond=(status,body)=>{res.writeHead(status,{'Content-Type':'application/json; charset=utf-8','Cache-Control':'no-store','Vary':'Authorization','X-Content-Type-Options':'nosniff','Content-Security-Policy':"default-src 'none'; frame-ancestors 'none'",'Referrer-Policy':'no-referrer',...(status===401?{'WWW-Authenticate':'Basic realm="PhotoBooth admin", charset="UTF-8"'}:{})});res.end(JSON.stringify(body));};
  const port=server.address()?.port;
  const hosts=new Set(['127.0.0.1:'+port,'localhost:'+port,...(tailnetHost?[tailnetHost]:[])]);
  if(!hosts.has(req.headers.host))return respond(403,{error:'host_not_allowed'});
  const origins=new Set(['http://127.0.0.1:'+port,'http://localhost:'+port,...(tailnetHost?['https://'+tailnetHost]:[])]);
  if(req.headers.origin&&!origins.has(req.headers.origin))return respond(403,{error:'origin_not_allowed'});
  if(!equal(req.headers.authorization,'Bearer '+token)&&!equal(req.headers.authorization,browserCredential))return respond(401,{error:'local_admin_token_required'});
  if(req.method!=='GET')return respond(405,{error:'read_only'});
  const url=new URL(req.url,'http://127.0.0.1');
  if(url.pathname==='/health')return respond(200,{service:'photobooth-admin',read_only:true,project});
  if(!['/api/report','/'].includes(url.pathname))return respond(404,{error:'not_found'});
  if(url.pathname==='/')return respond(200,{service:'photobooth-admin',read_only:true,report:'/api/report?days=7&fixtures=0',note:'Local API prepared for the later Mac dashboard.'});
  const days=Number(url.searchParams.get('days')??'7');const f=url.searchParams.get('fixtures')??'0';
  if(!Number.isInteger(days)||days<1||days>366||!['0','1'].includes(f))return respond(400,{error:'invalid_range'});
  const key=days+':'+f;
  try{
   const old=cache.get(key);if(old&&Date.now()-old.at<15000)return respond(200,old.report);
   if(!inFlight.has(key))inFlight.set(key,Promise.resolve().then(()=>read(days,f==='1')).finally(()=>inFlight.delete(key)));
   const report=await inFlight.get(key);if(cache.size>=4)cache.delete(cache.keys().next().value);cache.set(key,{at:Date.now(),report});respond(200,report);
  }catch{return respond(503,{error:'supabase_report_unavailable',hint:'Check the local Supabase CLI login and migration; no credentials are returned.'});}
 });
 return server;
}
if(require.main===module){
 (async()=>{
  const token=crypto.randomBytes(32).toString('hex');const directory=path.resolve(__dirname,'../../artifacts/admin-local');await fs.mkdir(directory,{recursive:true});
  const file=path.join(directory,'access-token');await fs.writeFile(file,token,{mode:0o600});
  const server=createAdminServer({token,tailnetHost:process.env.PHOTOBOOTH_ADMIN_HOST});
  const port=Number(process.env.PHOTOBOOTH_ADMIN_PORT||8787);if(!Number.isInteger(port)||port<1024||port>65535)throw new Error('Invalid local port');
  server.listen(port,'127.0.0.1',()=>console.log('Administration en lecture seule : http://127.0.0.1:'+port+' ; jeton local dans '+file));
  const close=()=>server.close(()=>process.exit(0));process.on('SIGINT',close);process.on('SIGTERM',close);
 })().catch(()=>{console.error('Cannot start local admin. Check the port and local configuration.');process.exitCode=1;});
}
module.exports={createAdminServer,readReport};
