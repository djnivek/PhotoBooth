const fs=require('node:fs');
const path=require('node:path');
const assert=require('node:assert/strict');
const {spawn}=require('node:child_process');
const {chromium}=require('../tests/browser/node_modules/playwright-core');
const root=path.resolve(__dirname,'..');
const runOutput=path.join(root,'artifacts/direct-square-0.6.0/live-smoke');
fs.mkdirSync(runOutput,{recursive:true});
const started=Date.now();
const child=spawn('dotnet',['run','--project','tools/PhotoBooth.CloudSmoke','--configuration','Release','--no-build','--','--share-synthetic-progressive-browser'],{cwd:root,windowsHide:true,stdio:['ignore','pipe','pipe']});
let output='';let code=null;
child.stdout.on('data',data=>output+=data);child.stderr.on('data',data=>output+=data);
const done=new Promise(resolve=>child.on('exit',value=>{code=value;resolve(value);}));
const delay=ms=>new Promise(resolve=>setTimeout(resolve,ms));
(async()=>{
 const sessionFile=path.join(runOutput,'browser-session.json');
 for(let i=0;i<300;i++){
  if(fs.existsSync(sessionFile)&&fs.statSync(sessionFile).mtimeMs>=started)break;
  if(code!==null)throw new Error('Share test ended early: '+output);
  if(i===299)throw new Error('Timed out awaiting the partial gallery');
  await delay(200);
 }
 const {session_id}=JSON.parse(fs.readFileSync(sessionFile,'utf8'));
 const sessionPath=path.join(runOutput,session_id.replaceAll('-',''));
 const token=fs.readFileSync(path.join(sessionPath,'share-token.txt'),'utf8').trim();
 const browser=await chromium.launch();const cases=[];const pages=[];
 try{
  for(const viewport of [{width:1280,height:900},{width:390,height:844}]){
   const page=await browser.newPage({viewport});let resolves=0;const errors=[];
   page.on('pageerror',e=>errors.push(e.message));
   await page.route('**/*',route=>{
    const req=route.request();const url=new URL(req.url());
    if(url.pathname.includes('/photos/')||url.hostname==='api.openai.com')return route.abort();
    if(req.method()==='POST'){
     assert.ok(url.pathname.endsWith('/downloads/resolve'));assert.equal(req.headers().authorization,undefined);resolves++;
    }
    return route.continue();
   });
   await page.goto('https://colors-party-estelle.vercel.app/#'+token);
   await page.locator('#waiting').waitFor({state:'visible'});
   assert.equal(await page.locator('.waiting-photo').count(),3);
   assert.equal(await page.locator('#photos figure').count(),5);
   assert.equal(await page.locator('#archive').isVisible(),false);
   await page.evaluate(()=>Promise.all([document.fonts.ready,...Array.from(document.images).map(img=>img.decode())]));
   assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
   await page.screenshot({path:path.join(runOutput,'progressive-'+viewport.width+'-waiting.png'),fullPage:true});
   pages.push({page,viewport,errors,initialUrl:page.url(),initialResolves:resolves,getResolves:()=>resolves});
  }
  fs.writeFileSync(path.join(sessionPath,'browser-continue.signal'),'continue');
  for(const item of pages){
   const {page,viewport}=item;
   await page.locator('#archive').waitFor({state:'visible',timeout:90000});
   assert.ok(item.getResolves()>item.initialResolves);
   assert.equal(page.url(),item.initialUrl);
   assert.equal(await page.locator('#waiting').isVisible(),false);
   assert.equal(await page.locator('#photos .pose').count(),4);
   assert.equal(await page.locator('#photos figure').count(),8);
   await page.locator('img').evaluateAll(images=>images.forEach(img=>img.loading='eager'));
   await page.evaluate(()=>Promise.all(Array.from(document.images).map(img=>img.decode())));
   assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
   assert.deepEqual(item.errors,[]);
   await page.screenshot({path:path.join(runOutput,'progressive-'+viewport.width+'-ready.png'),fullPage:true});
   cases.push({viewport,initial_results:1,final_results:4,final_originals:4,automatic_refresh:true,stable_url:true,overflow:false,script_errors:0,success:true});
  }
 }finally{await browser.close();}
 const result=await done;if(result!==0)throw new Error('Share verification failed: '+output);
 fs.writeFileSync(path.join(runOutput,'progressive-browser.json'),JSON.stringify({utc:new Date().toISOString(),production_gallery:true,production_api:true,synthetic_photos:true,openai_calls:0,cases,success:true},null,2));
 console.log('PASS production desktop/mobile: one result arrives first, then four results and archive appear through automatic polling at the same QR URL; no generation calls.');
 console.log(output.trim());
})().catch(error=>{child.kill();console.error(error.message.replace(/#[a-f0-9]{64}/g,'#[redacted]').replace(/token=[^\s"']+/g,'token=[redacted]'));process.exitCode=1;});
