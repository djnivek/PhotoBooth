// Full viewer journey in Chromium; clock advances never change shipping durations.
const {chromium}=require('playwright-core');
const http=require('node:http'),fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),output=path.join(root,'.impeccable/review/viewer');fs.mkdirSync(output,{recursive:true});fs.mkdirSync(path.join(root,'artifacts/viewer-0.8.0'),{recursive:true});
const server=http.createServer((req,res)=>{for(const h of JSON.parse(fs.readFileSync(path.join(root,'web/gallery/vercel.json'),'utf8')).headers[0].headers)res.setHeader(h.key,h.value);const relative=decodeURIComponent(req.url.split('?')[0]);if(relative.includes('..')){res.writeHead(404).end();return;}try{const file=path.join(root,'web/gallery',relative.endsWith('/')?relative+'index.html':relative);res.setHeader('Content-Type',({'.mjs':'text/javascript','.js':'text/javascript','.css':'text/css','.html':'text/html','.png':'image/png','.ttf':'font/ttf'})[path.extname(file)]||'application/octet-stream');res.end(fs.readFileSync(file));}catch{res.writeHead(404).end();}});
const origin='https://frkckrvsdsiwivdfyjau.supabase.co';
const make=(id,count=1)=>({id,photo_count:count,mode:'surprise',style:'halloween',released_at:new Date().toISOString(),photos:Array.from({length:count},(_,i)=>({name:`photo-${i+1}.jpg`}))});
const first=make('e3ac9960-328c-4a62-8d68-1266361bb8cf'),second=make('a3ac9960-328c-4a62-8d68-1266361bb8cf',4);
(async()=>{await new Promise(r=>server.listen(0,'127.0.0.1',r));const local=`http://127.0.0.1:${server.address().port}`;const browser=await chromium.launch();const reports=[];
try {for(const viewport of [{width:1920,height:1080},{width:1280,height:720},{width:390,height:844}]){
 const page=await browser.newPage({viewport});page.setDefaultTimeout(10000);await page.clock.install({time:new Date("2026-10-31T12:00:00Z")});const errors=[];page.on('pageerror',e=>errors.push(e.message));let feed=[first],enabled=true,offline=false;const requested=[];
 await page.route(origin+'/**',async route=>{const req=route.request();assert.equal(req.headers().authorization,undefined);if(offline)return route.abort('internetdisconnected');const headers={'Access-Control-Allow-Origin':local};
 if(req.method()==='OPTIONS')return route.fulfill({status:204,headers:{...headers,'Access-Control-Allow-Methods':'POST','Access-Control-Allow-Headers':'content-type'}});
 if(req.url().endsWith('/viewer/feed'))return route.fulfill({headers,json:{enabled,sessions:enabled?feed:[],revision:'fixture'}});
 if(req.url().endsWith('/viewer/photos')){const id=JSON.parse(req.postData()).session_id;requested.push(id);const s=feed.find(s=>s.id===id);return route.fulfill({headers,json:{enabled:true,id,photos:s.photos.map(p=>({...p,url:origin+`/storage/v1/object/sign/booth-private/${id}/${p.name}?token=fixture`}))}});}
 assert.ok(req.url().includes('/photo-'));assert.ok(!req.url().includes('/original-'));return route.fulfill({headers,contentType:'image/jpeg',body:fs.readFileSync(path.join(root,'artifacts/validation/single-pose-4x6.jpg'))});
 });
 await page.goto(local+'/viewer/');await page.clock.pauseAt(new Date('2026-10-31T12:00:10Z'));assert.match(await page.locator('#detail').textContent(),/lien de projection/);
 await page.goto(local+'/viewer/#'+'a'.repeat(64));await page.locator('#countdown').waitFor({state:'visible'});await page.evaluate(()=>document.fonts.ready);await page.screenshot({path:path.join(output,`announcement-${viewport.width}.png`),fullPage:true});
 await page.clock.runFor(16000);await page.locator('#photograph').waitFor({state:'visible'});await page.waitForFunction(()=>document.querySelector('#photo').naturalWidth>0);await page.screenshot({path:path.join(output,`photo-${viewport.width}.png`),fullPage:true});
 feed=[first,second];await page.clock.runFor(5000);assert.match(await page.locator('#caption').textContent(),/1 \/ 1/);assert.equal(await page.locator('#photograph').isVisible(),true);
 await page.clock.runFor(23900);assert.equal(await page.locator('#stage').getAttribute('data-phase'),'photo');await page.clock.runFor(2100);assert.equal(await page.locator('#stage').getAttribute('data-phase'),'announcement');
 await page.clock.runFor(16000);await page.locator('#photograph').waitFor({state:'visible'});assert.match(await page.locator('#caption').textContent(),/1 \/ 4/);
 for(let i=2;i<=4;i++){await page.clock.runFor(15250);assert.match(await page.locator('#caption').textContent(),new RegExp(i+' / 4'));}
 await page.clock.runFor(15250);await page.locator('#stage[data-phase=idle]').waitFor({state:'visible'});assert.equal(await page.locator('#stage').getAttribute('data-phase'),'idle');const cachedCalls=requested.length;
 await page.clock.runFor(30000);assert.equal(requested.length,cachedCalls);offline=true;await page.clock.runFor(5500);assert.match(await page.locator('#connection').textContent(),/interrompue/);assert.equal(await page.locator('#photograph').isVisible(),true);
 offline=false;enabled=false;await page.clock.runFor(5500);assert.match(await page.locator('#message').textContent(),/pause/);assert.equal(await page.locator('#photograph').isVisible(),false);
 enabled=true;await page.clock.runFor(5500);await page.locator('#stage[data-phase=idle]').waitFor({state:'visible'});assert.equal(await page.locator('#stage').getAttribute('data-phase'),'idle');
 const overflow=await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth||document.documentElement.scrollHeight>innerHeight);assert.equal(overflow,false);assert.deepEqual(errors,[]);
 reports.push({viewport,single_seconds:30,four_pose_seconds:15,announcement_seconds:15,queue_preserved:true,cache_reused:true,offline_reconnect:true,disable_clears:true,no_originals_requested:true,overflow:false,success:true});await page.close();
}fs.writeFileSync(path.join(root,'artifacts/viewer-0.8.0/browser.json'),JSON.stringify({synthetic:true,paid_api_calls:0,cases:reports,success:true},null,2));console.log('PASS viewer Chromium: queue, real image loading, clocks, offline, disable, cached random, 3 viewports.');
}finally{await browser.close();server.close();}})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
