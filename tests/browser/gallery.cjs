// Real Chromium rendering in CI. All API/photo requests are synthetic, never OpenAI.
const {chromium}=require('playwright-core');
const http=require('node:http'); const fs=require('node:fs'); const path=require('node:path');
const assert=require('node:assert/strict'); const {execFileSync}=require('node:child_process');
const root=path.resolve(__dirname,'../..');const output=path.join(root,'artifacts/gallery');fs.mkdirSync(output,{recursive:true});
const base='ee56e3b76bcc0472a3e4c321c5c54f61926c237b';
const server=http.createServer((req,res)=>{
 const before=req.url.startsWith('/before/');const relative=decodeURIComponent(req.url.split('?')[0]).replace(/^\/(before\/)?/,'')||'index.html';
 if(relative.includes('..')){res.writeHead(404);res.end();return;}
 try{
  const content=before?execFileSync('git',['show',`${base}:web/gallery/${relative}`],{cwd:root,stdio:['ignore','pipe','pipe']}):fs.readFileSync(path.join(root,'web/gallery',relative));
  const type={'.html':'text/html','.js':'text/javascript','.css':'text/css','.png':'image/png','.ttf':'font/ttf'}[path.extname(relative)]||'application/octet-stream';res.setHeader('Content-Type',type);res.end(content);
 }catch{res.writeHead(404);res.end();}
});
const origin='https://frkckrvsdsiwivdfyjau.supabase.co';const sid='e3ac9960-328c-4a62-8d68-1266361bb8cf';
const payload=(count,edited)=>({mode:edited?'surprise':'original',photo_count:count,expires_at:'2026-11-06T12:00:00Z',assets:[...Array.from({length:count},(_,i)=>`photo-${i+1}.jpg`),...(edited?Array.from({length:count},(_,i)=>`original-${i+1}.jpg`):[]),'sheet.jpg','archive.zip'].map(name=>({name,url:`${origin}/storage/v1/object/sign/booth-private/${sid}/${name}?token=fixture`}))});
(async()=>{
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const local=`http://127.0.0.1:${server.address().port}`;
 const browser=await chromium.launch();const reports=[];
 try{
  for(const viewport of [{width:1280,height:900},{width:390,height:844}]){
   const page=await browser.newPage({viewport});let count=4,edited=true,responseStatus=200,delayResolve,disconnected=false;let requests=0;
   const errors=[];page.on('pageerror',e=>errors.push(e.message));
   await page.route(origin+'/**',async route=>{
    const request=route.request();assert.equal(request.headers().authorization,undefined);
    if(disconnected)return route.abort("internetdisconnected");
    if(request.method()==="OPTIONS")return route.fulfill({status:204,headers:{"Access-Control-Allow-Origin":local,"Access-Control-Allow-Methods":"POST","Access-Control-Allow-Headers":"content-type"}});
    if(request.url().endsWith('/downloads/resolve')){requests++;assert.equal(request.method(),'POST');assert.equal(JSON.parse(request.postData()).token,'a'.repeat(64));if(delayResolve)await delayResolve;
      return route.fulfill({status:responseStatus,json:payload(count,edited),headers:{'Access-Control-Allow-Origin':local}});}
    assert.ok(request.url().includes('/storage/v1/object/sign/booth-private/'));
    const isSheet=request.url().includes('/sheet.jpg');
    return route.fulfill({contentType:'image/jpeg',body:fs.readFileSync(path.join(root,'artifacts/validation',isSheet?'four-poses-4x6.jpg':'single-pose-4x6.jpg'))});
   });
   const suffix=viewport.width===390?'mobile':'desktop';
   await page.goto(`${local}/before/#${'a'.repeat(64)}`);await page.locator('#archive').waitFor({state:'visible'});await page.locator("img").evaluateAll(images=>images.forEach(i=>i.loading="eager"));await page.evaluate(()=>Promise.all([document.fonts.ready,...Array.from(document.images).map(i=>i.decode().catch(()=>{}))]));
   await page.screenshot({path:path.join(output,`before-${suffix}.png`),fullPage:true});
   for(const c of [1,4])for(const e of [false,true]){
    count=c;edited=e;await page.goto(`${local}/#${'a'.repeat(64)}`);await page.locator('#archive').waitFor({state:'visible'});
    await page.locator("img").evaluateAll(images=>images.forEach(i=>i.loading="eager"));
    await page.evaluate(()=>Promise.all([document.fonts.ready,...Array.from(document.images).map(i=>i.complete?Promise.resolve():new Promise(r=>{i.onload=r;i.onerror=r;}))]));
    assert.equal(await page.locator('#photos .pose').count(),c);
    assert.equal(await page.locator('#photos figure').count(),c*(e?2:1));
    assert.ok(await page.locator('#photos img').evaluateAll(images=>images.every(i=>i.complete&&i.naturalWidth>0)));
    assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
    const requestBefore=requests;await page.getByRole('button',{name:'Actualiser les liens'}).click();await page.locator('#archive').waitFor({state:'visible'});assert.equal(requests,requestBefore+1);
    if(c===4&&e)await page.screenshot({path:path.join(output,`after-${suffix}.png`),fullPage:true});
    reports.push({viewport,count:c,edited:e,passed:true});
   }
   // Missing / expired / offline / loading states and recovery use the same page.
   await page.goto(local);await page.getByText(/Ce lien est incomplet/).waitFor();assert.equal(await page.locator('#archive').isVisible(),false);
   responseStatus=404;await page.goto(`${local}/#${'a'.repeat(64)}`);await page.getByText(/a expiré/).waitFor();await page.screenshot({path:path.join(output,`expired-${suffix}.png`),fullPage:true});
   responseStatus=200;await page.getByRole('button',{name:'Réessayer'}).click();await page.locator('#archive').waitFor({state:'visible'});
   disconnected=true;await page.context().setOffline(true);await page.getByRole('button',{name:'Actualiser les liens'}).click();await page.getByText(/connexion ne répond/).waitFor();assert.equal(await page.locator('#archive').isVisible(),false);disconnected=false;await page.context().setOffline(false);
   let release;delayResolve=new Promise(r=>release=r);await page.getByRole('button',{name:'Réessayer'}).click();await page.getByText('Ouverture de vos souvenirs…').waitFor();assert.equal(await page.locator('#retry').isDisabled(),true);release();delayResolve=null;await page.locator('#archive').waitFor({state:'visible'});
   await page.keyboard.press('Tab');assert.ok(await page.evaluate(()=>document.activeElement!==document.body));
   assert.deepEqual(errors,[]);await page.close();
  }
  fs.writeFileSync(path.join(output,'browser.json'),JSON.stringify({browser:'Chromium',api:'mocked',openai_calls:0,cases:reports,states:['missing','expired','offline','loading','recovery'],success:true},null,2));
  console.log('PASS Chromium: desktop/mobile, original/edited, one/four, signed download links, refresh, offline, expiry, loading, recovery, no overflow or script errors. No OpenAI calls.');
 }finally{await browser.close();server.close();}
})().catch(e=>{console.error(e);server.close();process.exitCode=1;});
