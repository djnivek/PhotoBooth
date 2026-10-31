import {viewerRoute,ViewerError,type ViewerDependencies} from './viewer.ts';
function assert(v:unknown):asserts v {if(!v)throw new Error('Assertion failed');}
const token='a'.repeat(64),owner='b'.repeat(64),id='e3ac9960-328c-4a62-8d68-1266361bb8cf';
function fixture(enabled=true) {
  const calls:{path:string;body:unknown}[]=[];
  const rows=[{id,photo_count:1,mode:'surprise',style:'halloween',released_at:new Date().toISOString(),photos:[{name:'photo-1.jpg',key:'private-result.jpg'}]}];
  const d:ViewerDependencies={hash:v=>Promise.resolve(v),signed:k=>Promise.resolve('https://signed.test/'+k),rest:<T>(path:string,_method?:string,body?:unknown)=>{
    calls.push({path,body});return Promise.resolve((path.includes('rpc/')?rows:path.includes('token_hash')?(path.includes(token)?[{owner_hash:owner,enabled}]:[]):path.startsWith('booth_sessions')?[{id}]:path.includes('enabled=eq.true')?(enabled?[{owner_hash:owner,enabled}]:[]):[]) as T);
  }};return {d,calls};
}
Deno.test('viewer capability is distinct, validated before DB and cannot authorize configuration or release',async()=>{
 const {d,calls}=fixture();
 for(const [path,body] of [['/viewer/settings',{token,enabled:true,include_tests:false}],[`/sessions/${id}/viewer`,{source:'camera'}]] as const){try{await viewerRoute(path,body,null,d);throw Error('accepted');}catch(e){assert(e instanceof ViewerError&&e.status===401);}}
 try{await viewerRoute('/viewer/feed',{token:'invalid'},null,d);throw Error('accepted');}catch(e){assert(e instanceof ViewerError&&e.status===404);}assert(calls.length===0);
});
Deno.test('viewer feed contains no object keys, credentials, originals, prompt or photo URLs',async()=>{
 const {d}=fixture();const result=await viewerRoute('/viewer/feed',{token},null,d);const json=JSON.stringify(result);assert(json.includes('photo-1.jpg'));assert(!json.includes('private-result')&&!json.includes('signed.test')&&!json.includes(owner));
 const photos=await viewerRoute('/viewer/photos',{token,session_id:id},null,d);assert(JSON.stringify(photos).includes('signed.test/private-result.jpg'));
});
Deno.test('disabled projection returns empty feed and cannot release a new session',async()=>{
 const {d,calls}=fixture(false);assert(JSON.stringify(await viewerRoute('/viewer/feed',{token},null,d))==='{"enabled":false,"sessions":[]}');
 const r=await viewerRoute(`/sessions/${id}/viewer`,{source:'camera'},owner,d);assert(JSON.stringify(r).includes('"scheduled":false'));assert(!calls.some(c=>c.path.startsWith('booth_projections')));
});
Deno.test('release is idempotent and never runs generation; settings hash capability',async()=>{
 const {d,calls}=fixture();await viewerRoute('/viewer/settings',{token,enabled:true,include_tests:false},owner,d);await viewerRoute(`/sessions/${id}/viewer`,{source:'fixture'},owner,d);
 assert(calls.some(c=>c.path==='booth_projections?on_conflict=session_id'));assert(calls.every(c=>!c.path.includes('jobs')&&!c.path.includes('openai')));
 const config=calls[0].body as Record<string,unknown>;assert(config.token_hash===token&&!Object.hasOwn(config,'token'));
});
Deno.test('viewer cannot resolve a session outside its eligible feed',async()=>{
 const {d}=fixture();try{await viewerRoute('/viewer/photos',{token,session_id:'a3ac9960-328c-4a62-8d68-1266361bb8cf'},null,d);throw Error('accepted');}catch(e){assert(e instanceof ViewerError&&e.status===404);}
});
