// Clock-driven queue: no DOM or network dependencies. Deadlines avoid timer drift.
export class ViewerQueue {
  constructor({now=()=>performance.now(),random=Math.random,seen=[],complete=()=>{}}={}) {
    this.now=now;this.random=random;this.seen=new Set(seen);this.complete=complete;
    this.library=[];this.queue=[];this.current=null;this.loaded=new Set();this.phase='empty';this.index=0;this.deadline=0;this.lastIdle='';
  }
  ingest(sessions) {
    const valid=sessions.filter(s=>typeof s.id==='string'&&[1,4].includes(s.photo_count)&&s.photos?.length===s.photo_count&&s.photos.every((p,i)=>p.name===`photo-${i+1}.jpg`));
    this.library=valid;
    const ids=new Set(valid.map(s=>s.id));
    this.queue=this.queue.filter(s=>ids.has(s.id));
    if(this.current&&!ids.has(this.current.id)) {this.current=null;this.phase='empty';}
    for(const s of valid)if(!this.seen.has(s.id)&&!this.queue.some(q=>q.id===s.id)&&!(this.current?.id===s.id&&this.phase!=='idle'))this.queue.push(s);
    return this.tick();
  }
  clear() {this.library=[];this.queue=[];this.current=null;this.loaded.clear();this.phase='empty';}
  ready(id) {this.loaded.add(id);return this.tick();}
  tick() {
    const now=this.now();
    if(this.phase==='photo'&&now>=this.deadline) {
      if(this.index+1<this.current.photo_count){this.index++;this.deadline=now+15000;}
      else {
        const id=this.current.id;this.seen.add(id);this.complete([...this.seen].slice(-2000));
        this.current=null;this.phase='empty';
      }
    }
    if((this.phase==='empty'||this.phase==='idle')&&this.queue.length) {
      this.current=this.queue.shift();this.index=0;this.phase='announcement';this.deadline=now+15000;
    }
    if((this.phase==='announcement'&&now>=this.deadline)||this.phase==='waiting') {
      if(this.loaded.has(this.current.id)){this.phase='photo';this.deadline=now+(this.current.photo_count===1?30000:15000);}
      else this.phase='waiting';
    }
    if((this.phase==='empty'||this.phase==='idle'&&now>=this.deadline)&&this.library.length) {
      const photos=this.library.flatMap(s=>s.photos.map((_,index)=>({s,index})));
      const options=photos.filter(p=>`${p.s.id}:${p.index}`!==this.lastIdle);
      const chosen=(options.length?options:photos)[Math.min(Math.floor(this.random()*(options.length||photos.length)),(options.length||photos.length)-1)];
      this.current=chosen.s;this.index=chosen.index;this.lastIdle=`${chosen.s.id}:${chosen.index}`;this.phase='idle';this.deadline=now+15000;
    }
    return {phase:this.phase,session:this.current,index:this.index,remaining:Math.max(0,Math.ceil((this.deadline-now)/1000)),duration:this.current?.photo_count===1&&this.phase==='photo'?30:15};
  }
}
