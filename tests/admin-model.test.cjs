const {test}=require('node:test');const assert=require('node:assert/strict');
const M=require('../web/admin/report-model.js');
const report=()=>({days:7,include_fixtures:false,generated_at:'2026-10-31T12:00:00Z',collection_started_at:null,totals:{sessions:0,captured:0,generated:0,download_requests:0,generation_failures:0,gallery_views:0,sessions_viewed:0,image_views:0,sessions_ready:0,sessions_failed:0,unclassified_events:0},timings:[],modes:[],chosen_themes:[],resolved_themes:[],download_assets:[],hourly:[]});
test('all shipped themes keep their French labels in monitoring',()=>{
 const catalog=require('../src/PhotoBooth.Core/styles.json');
 for(const theme of catalog.styles)assert.equal(M.label(theme.id),theme.label);
 assert.equal(M.label('unlisted_remote_theme'),'unlisted_remote_theme');
});
test('unknown durations and byte counts are never presented as zero',()=>{
 assert.equal(M.duration(null),'Non mesuré');assert.equal(M.duration(undefined),'Non mesuré');assert.equal(M.bytes(null),'Non mesuré');assert.equal(M.count(NaN),'Non mesuré');
 assert.equal(M.duration(0),'0 ms');assert.equal(M.duration(12750),'12,8 s');assert.equal(M.bytes(1024**3),'1 Go');assert.equal(M.date(null),'Non mesuré');
});
test('invalid and partial reports fail closed instead of rendering invented totals',()=>{
 assert.equal(M.validate(report()).totals.captured,0);
 for(const mutate of [r=>delete r.totals.captured,r=>r.totals.sessions=-1,r=>r.timings=null,r=>r.generated_at='invalid',r=>r.days='7']){
  const r=report();mutate(r);assert.throws(()=>M.validate(r));
 }
});
test('activity preserves all counts, fills empty periods, groups by Paris day across midnight and DST',()=>{
 const r=report();r.hourly=[{hour:'2026-10-30T22:00:00Z',sessions:1,captured:4,generated:2},{hour:'2026-10-30T23:00:00Z',sessions:2,captured:8,generated:4},{hour:'2026-10-31T00:00:00Z',sessions:3,captured:12,generated:6}];
 const bins=M.activity(r);assert.equal(bins.length,8);assert.equal(bins.find(b=>b.key==='2026-10-30').captured,4);assert.equal(bins.find(b=>b.key==='2026-10-31').captured,20);assert.equal(bins.reduce((s,b)=>s+b.sessions,0),6);
 const hourly={...r,days:1,generated_at:'2026-10-25T12:30:00Z',hourly:[{hour:'2026-10-25T00:00:00Z',sessions:1},{hour:'2026-10-25T01:00:00Z',sessions:2}]};
 const hourBins=M.activity(hourly);assert.equal(hourBins.length,25);assert.equal(hourBins.reduce((s,b)=>s+b.sessions,0),3);assert.equal(hourBins.filter(b=>b.sessions).length,2);
});
test('asset breakdown groups originals, transformations, sheets and archives without exposing paths',()=>{
 assert.deepEqual(M.downloadGroups([{asset:'original-1.jpg',requests:2},{asset:'original-4.jpg',requests:3},{asset:'photo-1.jpg',requests:7},{asset:'sheet.jpg',requests:4},{asset:'archive.zip',requests:1},{asset:'unknown',requests:2}]),{originals:5,transformed:7,sheets:4,archives:1,other:2});
});
test('unknown themes stay readable and old generation-only records never invent sessions',()=>{
 assert.equal(M.label('colors_party'),'Colors Party');assert.equal(M.label('future_theme'),'future_theme');assert.equal(M.label(null),'Non renseigné');
 const r=report();r.totals.generated=1;r.totals.unclassified_events=1;assert.equal(M.validate(r).totals.sessions,0);assert.equal(M.activity(r).every(b=>b.sessions===0),true);
});
test('rich report validates while malformed nested rows cannot partly replace the last report',()=>{
 const rich=require('./fixtures/admin-report.cjs')();assert.equal(M.validate(rich),rich);
 for(const [key,row] of [['timings',{kind:'generation',avg_ms:-1}],['hourly',{hour:'invalid'}],['chosen_themes',null],['modes',{mode:'original',sessions:'12'}]]){
  const invalid=report();invalid[key]=[row];assert.throws(()=>M.validate(invalid));
 }
});
