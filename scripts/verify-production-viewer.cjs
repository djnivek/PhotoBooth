// Uses only a temporary viewer capability created by the synthetic live check.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require('../tests/browser/node_modules/playwright-core');
const directory=path.resolve(__dirname,'../artifacts/viewer-0.8.0/live');
(async()=>{
 const input=path.join(directory,'browser-session.json'),until=Date.now()+90000;
 while(!fs.existsSync(input)){if(Date.now()>until)throw Error('Live setup did not finish');await new Promise(r=>setTimeout(r,250));}
 const setup=JSON.parse(fs.readFileSync(input,'utf8').replace(/^\uFEFF/,''));
 const url=new URL(setup.url);assert.equal(url.origin,'https://colors-party-estelle.vercel.app');assert.equal(url.pathname,'/viewer/');
 const browser=await chromium.launch(),reports=[];
 try {
  for(const viewport of [{width:1920,height:1080},{width:1280,height:720}]){
   const page=await browser.newPage({viewport});page.setDefaultTimeout(25000);await page.clock.install();const errors=[];let forbidden=false,provider=false;
   page.on('pageerror',e=>errors.push(e.message));
   page.on('request',req=>{const u=new URL(req.url());if(u.hostname==='api.openai.com')provider=true;if(u.hostname.endsWith('.supabase.co')){if(req.headers().authorization)forbidden=true;if(/\/(original-|input-|sheet\.|archive\.)/.test(u.pathname))forbidden=true;}});
   await page.goto(setup.url);await page.locator('#countdown').waitFor({state:'visible'});await page.clock.runFor(16000);await page.locator('#photograph').waitFor({state:'visible'});await page.waitForFunction(()=>document.querySelector('#photo').naturalWidth>0);
   const first=await page.locator('#caption').textContent();assert.match(first,/Au naturel.*1 \/ 1/);
   await page.clock.runFor(31000);await page.locator('#countdown').waitFor({state:'visible'});await page.clock.runFor(16000);await page.locator('#photograph').waitFor({state:'visible'});assert.match(await page.locator('#caption').textContent(),/Halloween.*1 \/ 4/);
   for(let i=2;i<=4;i++){await page.clock.runFor(15000);assert.match(await page.locator('#caption').textContent(),new RegExp(i+' / 4'));}
   assert.equal(forbidden,false);assert.equal(provider,false);assert.deepEqual(errors,[]);
   reports.push({viewport,real_api:true,real_signed_images:true,natural_then_four_results:true,no_archived_originals:true,no_provider_calls:true,success:true});await page.close();
  }
  fs.writeFileSync(path.join(directory,'browser-done.json'),JSON.stringify({success:true,synthetic:true,cases:reports},null,2));console.log('PASS production viewer: real feed and signed photos; natural then four results, full HD and 720p, no generation.');
 }finally{await browser.close();}
})().catch(()=>{console.error('Production viewer check failed; inspect local diagnostics without sharing capability URLs.');process.exitCode=1;});
