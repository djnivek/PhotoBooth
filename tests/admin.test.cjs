const {test}=require('node:test');const assert=require('node:assert/strict');
const {createAdminServer,readReport,loadOrCreateToken}=require('../tools/admin-local/server.cjs');
const fs=require('node:fs/promises');const path=require('node:path');const os=require('node:os');
const token='test-only-local-token-that-is-not-a-real-secret';
test('admin binds locally, authenticates, refuses hostile origins and never permits writes',async()=>{
 let calls=0;const server=createAdminServer({token,read:async(days,fixtures)=>{calls++;return {days,include_fixtures:fixtures,totals:{captured:4},timings:[]};}});
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const base='http://127.0.0.1:'+server.address().port;
 try{
  assert.equal(server.address().address,'127.0.0.1');
  assert.equal((await fetch(base+'/api/report')).status,401);
  const headers={Authorization:'Bearer '+token};
  assert.equal((await fetch(base+'/api/report',{headers:{...headers,Origin:'https://attacker.invalid'}})).status,403);
  assert.equal((await fetch(base+'/api/report',{method:'POST',headers})).status,405);
  assert.equal((await fetch(base+'/api/report?days=0',{headers})).status,400);
  assert.equal((await fetch(base+'/api/report?days=7;drop table x',{headers})).status,400);
  assert.equal((await fetch(base+'/api/report?fixtures=true',{headers})).status,400);
  assert.equal(calls,0);
  const response=await fetch(base+'/api/report?days=7&fixtures=1',{headers});
  assert.equal(response.headers.get('cache-control'),'no-store');assert.equal(response.headers.get('access-control-allow-origin'),null);
  assert.deepEqual(await response.json(),{days:7,include_fixtures:true,totals:{captured:4},timings:[]});
  await Promise.all([fetch(base+'/api/report?days=7&fixtures=1',{headers}),fetch(base+'/api/report?days=7&fixtures=1',{headers})]);assert.equal(calls,1);
 }finally{await new Promise(resolve=>server.close(resolve));}
});
test('report failure is explicit and cannot leak CLI stderr or credentials',async()=>{
 const server=createAdminServer({token,read:()=>{throw new Error('private stdout fake-provider-key');}});await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 try{const r=await fetch('http://127.0.0.1:'+server.address().port+'/api/report',{headers:{Authorization:'Bearer '+token}});assert.equal(r.status,503);assert.ok(!(await r.text()).includes('fake-provider-key'));}
 finally{await new Promise(resolve=>server.close(resolve));}
});
test('weak tokens and arbitrary network hostnames are rejected',()=>{
 assert.throws(()=>createAdminServer({token:'short'}));assert.throws(()=>createAdminServer({token,tailnetHost:'public.example.com'}));
});
test('browser login uses the same strong token and retains host, origin and write protections',async()=>{
 const host='photobooth.example.ts.net';const server=createAdminServer({token,tailnetHost:host,read:async()=>({totals:{captured:4},timings:[]})});
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const base='http://127.0.0.1:'+server.address().port;
 const basic=(user,password)=>'Basic '+Buffer.from(user+':'+password).toString('base64');
 try{
  const denied=await fetch(base+'/api/report');assert.equal(denied.status,401);assert.equal(denied.headers.get('www-authenticate'),null);
  for(const credential of [basic('admin','wrong'),basic('guest',token),'Basic malformed'])assert.equal((await fetch(base+'/api/report',{headers:{Authorization:credential}})).status,401);
  const headers={Authorization:basic('admin',token)};
  const response=await fetch(base+'/api/report',{headers});assert.equal(response.status,200);assert.equal((await response.json()).totals.captured,4);
  // fetch normalizes Host; use a real HTTP request to exercise the host allowlist.
  const hostStatus=hostHeader=>new Promise((resolve,reject)=>{const req=require('node:http').request(base+'/health',{headers:{...headers,Host:hostHeader}},res=>{res.resume();res.on('end',()=>resolve(res.statusCode));});req.on('error',reject);req.end();});
  assert.equal(await hostStatus(host),200);assert.equal(await hostStatus('attacker.invalid'),403);
  assert.equal((await fetch(base+'/api/report',{headers:{...headers,Origin:'https://attacker.invalid'}})).status,403);
  assert.equal((await fetch(base+'/api/report',{headers,method:'POST'})).status,405);
 }finally{await new Promise(resolve=>server.close(resolve));}
});
test('report query supports the current CLI and refuses an unlinked or different project before execution',async()=>{
 const cwd=await fs.mkdtemp(path.join(os.tmpdir(),'photobooth-admin-query-test-'));let calls=0;let queryFile;let rowArray=false;
 const execute=async(cli,args,options)=>{
  calls++;assert.equal(cli,'supabase');assert.equal(options.cwd,cwd);assert.ok(!args.includes('--project-ref'));assert.ok(args.includes('--linked'));
  queryFile=args[args.indexOf('--file')+1];const sql=await fs.readFile(queryFile,'utf8');assert.match(sql,/booth_monitoring_report\(7, false\)/);assert.match(sql,/storage.objects/);assert.ok(!sql.includes('__DAYS__'));assert.ok(!/\b(insert|update|delete|drop|alter|create)\s/i.test(sql));
  const rows=[{report:{totals:{captured:4},timings:[]}}];return {stdout:JSON.stringify(rowArray?rows:{rows})};
 };
 try{
  await assert.rejects(readReport(7,false,{cwd,execute}));assert.equal(calls,0);
  await fs.mkdir(path.join(cwd,'supabase/.temp'),{recursive:true});const linked=path.join(cwd,'supabase/.temp/project-ref');
  await fs.writeFile(linked,'another-project');await assert.rejects(readReport(7,false,{cwd,execute}),/PhotoBooth project/);assert.equal(calls,0);
  await fs.writeFile(linked,'frkckrvsdsiwivdfyjau\n');assert.equal((await readReport(7,false,{cwd,execute})).totals.captured,4);assert.equal(calls,1);
  await assert.rejects(fs.stat(queryFile),{code:'ENOENT'});
  rowArray=true;assert.equal((await readReport(7,false,{cwd,execute})).totals.captured,4);assert.equal(calls,2);
  await assert.rejects(readReport(0,false,{cwd,execute}),/Invalid report range/);assert.equal(calls,2);
 }finally{await fs.rm(cwd,{recursive:true,force:true});}
});
async function listening(options, work) {
 const server=createAdminServer({token,...options});
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 const base='http://127.0.0.1:'+server.address().port;
 try{await work(base,server);}finally{await new Promise(resolve=>server.close(resolve));}
}
function login(base,password=token,headers={}){
 return fetch(base+'/api/session',{method:'POST',headers:{Origin:base,'Content-Type':'application/json','X-Photobooth-Admin':'1',...headers},body:JSON.stringify({token:password})});
}
test('public login shell contains no report or credential, assets stay inside prefix and old browser link redirects',async()=>{
 await listening({basePath:'/photobooth'},async base=>{
  for(const route of ['/','/photobooth/']){
   const r=await fetch(base+route);assert.equal(r.status,200);const html=await r.text();
   assert.match(html,/Code admin/);assert.match(html,/src="\/photobooth\/dashboard.js"/);assert.ok(!html.includes(token));assert.ok(!html.includes('__BASE__'));
   assert.match(r.headers.get('content-security-policy'),/frame-ancestors 'none'/);assert.equal(r.headers.get('cache-control'),'no-store');
  }
  const redirect=await fetch(base+'/api/report?days=7',{headers:{Accept:'text/html'},redirect:'manual'});
  assert.equal(redirect.status,303);assert.equal(redirect.headers.get('location'),'/photobooth/');
  assert.equal((await fetch(base+'/api/report')).status,401);
  for(const route of ['/style.css','/dashboard.js','/report-model.js','/assets/wordmark.png'])assert.equal((await fetch(base+'/photobooth'+route)).status,200);
  for(const route of ['/artifacts/admin-local/access-token','/report.sql','/%2e%2e/tools/admin-local/server.cjs'])assert.notEqual((await fetch(base+route)).status,200);
 });
});
test('form login creates a scoped HttpOnly session; reload works, logout revokes it, expiry requires login',async()=>{
 let time=1000;
 await listening({basePath:'/photobooth',now:()=>time,sessionMs:60000,read:async()=>({totals:{captured:4},timings:[]})},async base=>{
  assert.equal((await login(base,'wrong')).status,401);
  const r=await login(base);assert.equal(r.status,200);const set=r.headers.get('set-cookie');
  assert.match(set,/HttpOnly/);assert.match(set,/SameSite=Strict/);assert.match(set,/Path=\/photobooth/);assert.ok(!set.includes(token));
  const headers={Cookie:set.split(';')[0]};
  assert.equal((await fetch(base+'/api/session',{headers})).status,200);
  assert.equal((await fetch(base+'/api/report',{headers})).status,200);
  const download=await fetch(base+'/api/report?download=1',{headers:{...headers,Accept:'text/html'},redirect:'manual'});
  assert.equal(download.status,200);assert.match(download.headers.get('content-disposition'),/attachment/);assert.equal((await download.json()).totals.captured,4);
  assert.equal((await fetch(base+'/api/logout',{method:'POST',headers})).status,403);
  const logout=await fetch(base+'/api/logout',{method:'POST',headers:{...headers,Origin:base,'X-Photobooth-Admin':'1'}});
  assert.equal(logout.status,200);assert.match(logout.headers.get('set-cookie'),/Max-Age=0/);
  assert.equal((await fetch(base+'/api/report',{headers})).status,401);
  const again=await login(base);const cookie=again.headers.get('set-cookie').split(';')[0];time+=60001;
  assert.equal((await fetch(base+'/api/session',{headers:{Cookie:cookie}})).status,401);
 });
});
test('login enforces origin, CSRF header, bounded JSON, and a one-minute failed-attempt lockout',async()=>{
 let time=1000;
 await listening({now:()=>time},async base=>{
  assert.equal((await login(base,token,{Origin:'https://attacker.invalid'})).status,403);
  assert.equal((await login(base,token,{'X-Photobooth-Admin':'0'})).status,403);
  assert.equal((await login(base,token,{'Content-Type':'text/plain'})).status,415);
  assert.equal((await login(base,'x'.repeat(2200))).status,413);
  for(let i=0;i<5;i++)assert.equal((await login(base,'wrong')).status,401);
  const blocked=await login(base);assert.equal(blocked.status,429);assert.equal(blocked.headers.get('retry-after'),'60');
  time+=60001;assert.equal((await login(base)).status,200);
 });
});
test('Tailscale login issues Secure cookies and refuses a mismatched origin',async()=>{
 const host='photobooth.example.ts.net';
 await listening({tailnetHost:host,basePath:'/photobooth'},async(base)=>{
  const request=origin=>new Promise((resolve,reject)=>{
   const req=require('node:http').request(base+'/api/session',{method:'POST',headers:{Host:host,Origin:origin,'X-Photobooth-Admin':'1','Content-Type':'application/json'}},res=>{res.resume();res.on('end',()=>resolve(res));});req.on('error',reject);req.end(JSON.stringify({token}));
  });
  const r=await request('https://'+host);assert.equal(r.statusCode,200);assert.match(r.headers['set-cookie'][0],/; Secure$/);
  assert.equal((await request(base)).statusCode,403);
 });
});
test('local code survives restart, retains private permissions, and invalid files are not silently replaced',async()=>{
 const cwd=await fs.mkdtemp(path.join(os.tmpdir(),'photobooth-admin-token-'));const file=path.join(cwd,'token');
 try{
  const first=await loadOrCreateToken(file);assert.match(first,/^[a-f0-9]{64}$/);assert.equal(await loadOrCreateToken(file),first);
  if(process.platform!=='win32')assert.equal((await fs.stat(file)).mode&0o777,0o600);
  await fs.writeFile(file,'invalid');await assert.rejects(loadOrCreateToken(file),/Invalid local admin code/);assert.equal(await fs.readFile(file,'utf8'),'invalid');
 }finally{await fs.rm(cwd,{recursive:true,force:true});}
});
test('concurrent report reads share one query and failures can be explicitly retried',async()=>{
 let calls=0;let complete;
 await listening({read:()=>{calls++;return new Promise(resolve=>{complete=resolve;});}},async base=>{
  const headers={Authorization:'Bearer '+token};const requests=[fetch(base+'/api/report',{headers}),fetch(base+'/api/report',{headers})];
  while(!complete)await new Promise(resolve=>setTimeout(resolve,5));
  complete({totals:{captured:4},timings:[]});await Promise.all(requests);assert.equal(calls,1);
 });
 let calls2=0;
 await listening({read:async()=>{if(++calls2===1)throw new Error('private');return {totals:{},timings:[]};}},async base=>{
  const headers={Authorization:'Bearer '+token};assert.equal((await fetch(base+'/api/report',{headers})).status,503);assert.equal((await fetch(base+'/api/report',{headers})).status,200);
 });
});
