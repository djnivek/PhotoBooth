const {test}=require('node:test');const assert=require('node:assert/strict');
const {createAdminServer}=require('../tools/admin-local/server.cjs');
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
