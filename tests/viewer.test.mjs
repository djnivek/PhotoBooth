import test from 'node:test';
import assert from 'node:assert/strict';
import {ViewerQueue} from '../web/gallery/viewer/engine.mjs';
const session=(id,count=1)=>({id,photo_count:count,mode:'surprise',style:'halloween',photos:Array.from({length:count},(_,i)=>({name:`photo-${i+1}.jpg`}))});
test('single: 15 second announcement, full 30 second photo, then idle without replay',()=>{
 let t=0,saved=[];const q=new ViewerQueue({now:()=>t,random:()=>0,complete:x=>saved=x});const a=session('a');
 assert.equal(q.ingest([a]).phase,'announcement');q.ready('a');t=14999;assert.equal(q.tick().phase,'announcement');t=15000;assert.equal(q.tick().phase,'photo');
 t=44999;assert.equal(q.tick().phase,'photo');t=45000;assert.equal(q.tick().phase,'idle');assert.deepEqual(saved,['a']);assert.equal(q.ingest([a]).phase,'idle');
});
test('four poses: preserve order, 15 seconds each and queue new séance without interruption',()=>{
 let t=0;const q=new ViewerQueue({now:()=>t});const a=session('a',4),b=session('b');q.ingest([a]);q.ready('a');t=15000;assert.equal(q.tick().index,0);
 t=16000;q.ingest([a,b]);q.ready('b');assert.equal(q.tick().session.id,'a');
 for(let i=1;i<4;i++){t=15000+i*15000;assert.equal(q.tick().index,i);assert.equal(q.tick().session.id,'a');}
 t=75000;assert.equal(q.tick().session.id,'b');assert.equal(q.tick().phase,'announcement');
});
test('idle yields immediately to a new ready session, without repeating the same idle photo',()=>{
 let t=0;const a=session('a',4),b=session('b');const q=new ViewerQueue({now:()=>t,random:()=>0,seen:['a']});q.ingest([a]);assert.equal(q.tick().phase,'idle');assert.equal(q.index,0);
 t=15000;q.tick();assert.equal(q.index,1);q.ingest([a,b]);assert.equal(q.phase,'announcement');assert.equal(q.current.id,'b');
});
test('slow photos do not consume their display time; disabling clears active and pending',()=>{
 let t=0;const q=new ViewerQueue({now:()=>t});q.ingest([session('a'),session('b')]);t=60000;assert.equal(q.tick().phase,'waiting');q.ready('a');assert.equal(q.tick().remaining,30);
 q.clear();assert.equal(q.tick().phase,'empty');assert.equal(q.queue.length,0);
});
test('partial poses and archived metamorphosis originals are rejected by the player',()=>{
 const q=new ViewerQueue();q.ingest([{...session('a',4),photos:[{name:'photo-1.jpg'}]},{...session('b'),photos:[{name:'original-1.jpg'}]}]);assert.equal(q.phase,'empty');
});
test('reload remembers completed sessions but still includes them in the idle library',()=>{
 const q=new ViewerQueue({seen:['a']});q.ingest([session('a')]);assert.equal(q.phase,'idle');assert.equal(q.queue.length,0);
});
