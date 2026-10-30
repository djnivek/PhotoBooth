const {test}=require('node:test');
const assert=require('node:assert/strict');
const vm=require('node:vm');
const fs=require('node:fs');
const source=fs.readFileSync('web/gallery/gallery.js','utf8');
class Element {
  constructor(tag){this.tag=tag;this.children=[];this.hidden=true;this.textContent='';this.listeners={};}
  append(...children){this.children.push(...children);}
  replaceChildren(...children){this.children=children;}
  addEventListener(event,callback){this.listeners[event]=callback;}
}
async function render({count=4,status=200,token='a'.repeat(64),origin='https://frkckrvsdsiwivdfyjau.supabase.co'}={}){
  const elements=Object.fromEntries(['status','retry','photos','archive','expiry'].map(name=>['#'+name,new Element(name)]));
  const requests=[];
  const context=vm.createContext({URL,Error,Date,location:{hash:'#'+token},
    document:{querySelector:name=>elements[name],createElement:tag=>new Element(tag)},
    fetch:async(url,options)=>{requests.push({url,options});return {ok:status===200,status,json:async()=>({
      expires_at:'2026-11-06T12:00:00Z',assets:[...Array.from({length:count},(_,i)=>`photo-${i+1}.jpg`),'sheet.jpg','archive.zip'].map(name=>({name,url:origin+'/storage/'+name}))
    })};}});
  vm.runInContext(source,context);
  await new Promise(resolve=>setImmediate(resolve));
  return {elements,requests};
}
for(const count of [1,4])test(`${count} individual photo links, sheet and complete archive`,async()=>{
  const {elements,requests}=await render({count});
  assert.equal(elements['#photos'].children.length,count+1);
  assert.equal(elements['#archive'].hidden,false);
  assert.match(elements['#archive'].href,/archive.zip$/);
  assert.equal(requests.length,1);
  assert.equal(requests[0].options.method,'POST');
  assert.equal(requests[0].options.credentials,'omit');
  assert.equal(JSON.parse(requests[0].options.body).token,'a'.repeat(64));
  assert.equal(requests[0].url.includes('a'.repeat(64)),false);
  const captions=elements['#photos'].children.map(figure=>figure.children[1].children[0].textContent);
  assert.equal(captions[0],'Photo 1');assert.equal(captions.at(-1),'Votre planche 4×6');
});
test('missing capability never sends a request',async()=>{
  const {elements,requests}=await render({token:''});assert.equal(requests.length,0);
  assert.match(elements['#status'].textContent,/incomplet/);assert.equal(elements['#archive'].hidden,true);
});
test('expired link is explicit and exposes no photo',async()=>{
  const {elements}=await render({status:404});assert.match(elements['#status'].textContent,/expiré/);
  assert.equal(elements['#photos'].children.length,0);
});
test('foreign storage address cannot become a download link',async()=>{
  const {elements}=await render({origin:'https://example.invalid'});
  assert.match(elements['#status'].textContent,/non reconnue/);assert.equal(elements['#archive'].hidden,true);
});
