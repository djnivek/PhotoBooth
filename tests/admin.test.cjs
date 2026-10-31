const {test}=require('node:test');const assert=require('node:assert/strict');
const {createAdminServer,readReport}=require('../tools/admin-local/server.cjs');
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
  const denied=await fetch(base+'/api/report');assert.equal(denied.status,401);assert.match(denied.headers.get('www-authenticate'),/^Basic realm="PhotoBooth admin"/);
  for(const credential of [basic('admin','wrong'),basic('guest',token),'Basic malformed'])assert.equal((await fetch(base+'/api/report',{headers:{Authorization:credential}})).status,401);
  const headers={Authorization:basic('admin',token),Host:host,Origin:'https://'+host};
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
  queryFile=args[args.indexOf('--file')+1];assert.equal(await fs.readFile(queryFile,'utf8'),'select public.booth_monitoring_report(7,false) as report;');
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
