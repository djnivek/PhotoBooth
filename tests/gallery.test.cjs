const {test}=require('node:test');
const assert=require('node:assert/strict');
const vm=require('node:vm');
const fs=require('node:fs');
const source=fs.readFileSync('web/gallery/gallery.js','utf8');
const origin='https://frkckrvsdsiwivdfyjau.supabase.co';
const id='e3ac9960-328c-4a62-8d68-1266361bb8cf';
class Element {
  constructor(tag){this.tag=tag;this.children=[];this.hidden=true;this.textContent='';this.listeners={};}
  append(...children){this.children.push(...children);}
  replaceChildren(...children){this.children=children;}
  addEventListener(event,callback){this.listeners[event]=callback;}
  setAttribute(name,value){this[name]=value;}
}
function payload(count,edited){return {photo_count:count,mode:edited?'surprise':'original',expires_at:'2026-11-06T12:00:00Z',assets:[
 ...Array.from({length:count},(_,i)=>`photo-${i+1}.jpg`),...(edited?Array.from({length:count},(_,i)=>`original-${i+1}.jpg`):[]),'sheet.jpg','archive.zip'
].map(name=>({name,url:`${origin}/storage/v1/object/sign/booth-private/${id}/${name}?token=signed`}))};}
async function render({count=4,edited=false,status=200,token='a'.repeat(64),modify=x=>x,offline=false}={}){
  const elements=Object.fromEntries(['status','retry','photos','sheet','archive','expiry'].map(name=>['#'+name,new Element(name)]));
  const requests=[];let currentStatus=status;const events={};let reloads=0;
  const context=vm.createContext({URL,Error,Date,AbortSignal,location:{hash:'#'+token,reload:()=>reloads++},addEventListener:(name,listener)=>events[name]=listener,
    document:{querySelector:name=>elements[name],createElement:tag=>new Element(tag)},
    fetch:async(url,options)=>{requests.push({url,options});if(offline)throw new TypeError('Network error');return {ok:currentStatus===200,status:currentStatus,json:async()=>modify(payload(count,edited))};}});
  vm.runInContext(source,context);
  await new Promise(resolve=>setImmediate(resolve));
  return {elements,requests,setStatus:value=>currentStatus=value,events,reloads:()=>reloads};
}
for(const count of [1,4])for(const edited of [false,true])test(`${count} poses, ${edited?'original/edited pairs':'original only'}, sheet and archive`,async()=>{
  const {elements,requests}=await render({count,edited});
  const sections=elements['#photos'].children;
  assert.equal(sections.length,count);
  sections.forEach((section,index)=>{
    const figures=section.children[1].children;
    assert.equal(figures.length,edited?2:1);
    assert.equal(figures[0].children[1].children[0].textContent,`Au naturel ${index+1}`);
    if(edited)assert.equal(figures[1].children[1].children[0].textContent,`Métamorphose ${index+1}`);
    figures.forEach(f=>assert.ok(new URL(f.children[1].children[1].href).searchParams.has('download')));
  });
  assert.equal(elements['#sheet'].hidden,false);assert.equal(elements['#archive'].hidden,false);
  assert.equal(requests.length,1);assert.equal(requests[0].options.method,'POST');
  assert.equal(requests[0].options.credentials,'omit');assert.equal(requests[0].options.headers.Authorization,undefined);
  assert.equal(JSON.parse(requests[0].options.body).token,'a'.repeat(64));assert.equal(requests[0].url.includes('a'.repeat(64)),false);
});
test('explicit fallback never labels originals as transformed',async()=>{
 const {elements}=await render({edited:true,modify:x=>({...x,used_original_fallback:true})});
 assert.equal(elements['#photos'].children[0].children[1].children.length,1);
 assert.match(elements['#status'].textContent,/au naturel/);
});
test('missing capability never sends a request',async()=>{
 const {elements,requests}=await render({token:''});assert.equal(requests.length,0);
 assert.match(elements['#status'].textContent,/incomplet/);assert.equal(elements['#retry'].hidden,true);
});
test('expired link is explicit and exposes no photo',async()=>{
 const {elements}=await render({status:404});assert.match(elements['#status'].textContent,/expiré/);assert.equal(elements['#photos'].children.length,0);
});
for(const address of ['https://example.invalid/file.jpg',origin+'/storage/v1/object/public/booth-private/file.jpg',origin+'/functions/v1/photobooth-api/sessions'])test(`untrusted asset blocked before any gallery mutation: ${address}`,async()=>{
 const {elements}=await render({modify:x=>{x.assets.at(-1).url=address;return x;}});
 assert.match(elements['#status'].textContent,/non reconnue/);assert.equal(elements['#archive'].hidden,true);assert.equal(elements['#photos'].children.length,0);
});
test('incomplete edited gallery exposes no partial result',async()=>{
 const {elements}=await render({edited:true,modify:x=>{x.assets=x.assets.filter(a=>a.name!=='original-4.jpg');return x;}});
 assert.match(elements['#status'].textContent,/incomplète/);assert.equal(elements['#photos'].children.length,0);
});
test('retry clears expired assets and never starts a generation',async()=>{
 const {elements,requests,setStatus}=await render();setStatus(404);await elements['#retry'].listeners.click();
 assert.equal(elements['#photos'].children.length,0);assert.equal(elements['#sheet'].hidden,true);assert.equal(elements['#archive'].hidden,true);
 assert.ok(requests.every(r=>r.url.endsWith('/downloads/resolve')));
});
test('offline gives a useful retry without credentials',async()=>{
 const {elements,requests}=await render({offline:true});assert.match(elements['#status'].textContent,/connexion/);
 assert.equal(elements['#retry'].hidden,false);assert.equal(requests[0].options.headers.Authorization,undefined);
});
test('opening a second QR in an existing tab reloads the capability',async()=>{
 const {events,reloads}=await render();events.hashchange();assert.equal(reloads(),1);
});
