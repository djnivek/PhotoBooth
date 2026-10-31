import {ViewerQueue} from './engine.mjs';
const api='https://frkckrvsdsiwivdfyjau.supabase.co/functions/v1/photobooth-api';
const token=location.hash.slice(1);
window.addEventListener('hashchange',()=>location.reload());
const $=id=>document.getElementById(id);
const phrases=['Quelqu’un vient de prendre la pose. Prêts à découvrir ?','De nouveaux souvenirs arrivent dans la salle.','La prochaine révélation, c’est maintenant.','La fête nous réserve de beaux portraits.'];
const labels={colors_party:'Colors Party',halloween:'Halloween',space:'Dans les étoiles',fantasy:'Pays imaginaire',elves:'Petits lutins',disco:'Disco velours'};
let queue,storageKey,lastView='',announced='',phrase=-1,revision='',enabled=false,busy=false,permanentError=false;
const cache=new Map(),pending=new Map(),retryAt=new Map();
async function request(route,body={}) {
  const response=await fetch(api+route,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({token,...body}),credentials:'omit',referrerPolicy:'no-referrer',cache:'no-store',signal:AbortSignal.timeout(12000)});
  if(!response.ok){const error=new Error('Connexion indisponible');error.status=response.status;throw error;}
  return response.json();
}
function state(message,detail) {
  $('caption').hidden=true;$('invitation').hidden=false;$('photograph').hidden=true;$('countdown').hidden=true;$('message').textContent=message;$('detail').textContent=detail;$('stage').dataset.phase='empty';$('progress').firstElementChild.style.width='0%';
}
function clearCache(){lastView='';for(const item of cache.values())for(const url of item.urls)URL.revokeObjectURL(url);cache.clear();$('photo').removeAttribute('src');}
async function load(session) {
  if(cache.has(session.id)){const hit=cache.get(session.id);cache.delete(session.id);cache.set(session.id,hit);return hit;}
  if(pending.has(session.id))return pending.get(session.id);
  const task=(async()=>{
    const reply=await request('/viewer/photos',{session_id:session.id});
    if(!reply.enabled){enabled=false;queue.clear();clearCache();throw new Error('Projection désactivée');}
    if(!Array.isArray(reply.photos)||reply.photos.length!==session.photo_count)throw new Error('Photos incomplètes');
    const urls=[];
    try {
      for(const [i,p] of reply.photos.entries()) {
        const u=new URL(p.url);
        if(p.name!==`photo-${i+1}.jpg`||u.origin!=='https://frkckrvsdsiwivdfyjau.supabase.co'||!u.pathname.startsWith('/storage/v1/object/sign/'))throw new Error('Photo indisponible');
        const response=await fetch(u,{credentials:'omit',referrerPolicy:'no-referrer',signal:AbortSignal.timeout(20000)});
        if(!response.ok)throw new Error('Téléchargement interrompu');
        const blob=await response.blob();if(blob.size>16777216||!blob.type.startsWith('image/'))throw new Error('Photo non valide');
        const url=URL.createObjectURL(blob);urls.push(url);const image=new Image();image.src=url;await image.decode();
      }
      if(!enabled){for(const url of urls)URL.revokeObjectURL(url);throw new Error('Projection désactivée');}
      const value={urls};cache.set(session.id,value);
      for(const id of cache.keys())if(cache.size>8&&id!==session.id&&id!==queue.current?.id){const old=cache.get(id);for(const url of old.urls)URL.revokeObjectURL(url);cache.delete(id);queue.loaded.delete(id);}
      return value;
    } catch(error){for(const url of urls)URL.revokeObjectURL(url);throw error;}
  })();
  pending.set(session.id,task);try{return await task;}finally{pending.delete(session.id);}
}
function render() {
  if(!enabled||permanentError)return;
  const s=queue.tick();
  if(!s.session){state('La fête prend la pose.','Les prochains souvenirs apparaîtront ici. À vous de jouer au photomaton !');return;}
  if(!cache.has(s.session.id)&&!pending.has(s.session.id)&&Date.now()>=(retryAt.get(s.session.id)||0)) {
    load(s.session).then(()=>{queue.ready(s.session.id);$('connection').textContent='';render();}).catch(()=>{retryAt.set(s.session.id,Date.now()+5000);$('connection').textContent='Photos en attente de connexion.';});
  }
  const key=`${s.phase}:${s.session.id}:${s.index}`;
  if(s.phase==='announcement') {
    if(announced!==s.session.id){announced=s.session.id;phrase=(phrase+1)%phrases.length;}
    if(lastView!==key) {state(phrases[phrase],'Rejoignez l’écran de la salle, vos photos vont apparaître.');$('stage').dataset.phase='announcement';}
    $('countdown').hidden=false;$('countdown').textContent=String(s.remaining);
  } else if(s.phase==='waiting'||!cache.has(s.session.id)) {
    state('Vos souvenirs arrivent.','Encore un instant, les photos se préparent pour le grand écran.');
  } else {
    if(lastView!==key||$('photo').src!==cache.get(s.session.id).urls[s.index]) {
      $('caption').hidden=false;$('invitation').hidden=true;$('photograph').hidden=false;$('stage').dataset.phase=s.phase;
      $('photo').src=cache.get(s.session.id).urls[s.index];
      $('photo').alt=`${s.session.mode==='surprise'?'Métamorphose':'Au naturel'}, pose ${s.index+1} sur ${s.session.photo_count}`;
      $('caption').textContent=`${s.session.mode==='original'?'Au naturel':labels[s.session.style]||'Métamorphose'} · ${s.index+1} / ${s.session.photo_count}`;
    }
    $('progress').firstElementChild.style.width=s.phase==='photo'?`${Math.min(100,(1-s.remaining/s.duration)*100)}%`:'0%';
  }
  lastView=key;
}
async function poll() {
  if(busy||permanentError)return;busy=true;
  try {
    const feed=await request('/viewer/feed',{known:revision});
    enabled=feed.enabled===true;
    if(!enabled){revision='';queue.clear();clearCache();state('La projection fait une pause.','La borne peut réactiver le diaporama depuis ses réglages.');}
    else {if(!feed.unchanged)queue.ingest(feed.sessions);revision=feed.revision;$('connection').textContent='';$('retry').hidden=true;render();}
  } catch(error) {
    if([401,404].includes(error.status)){permanentError=true;enabled=false;queue.clear();clearCache();state('Ce lien ne permet pas la projection.','Récupérez le lien du viewer dans les réglages du photomaton.');}
    else {$('connection').textContent='Connexion interrompue. Reconnexion automatique…';$('retry').hidden=false;if(!enabled)state('On retrouve la connexion.','Le diaporama reprendra automatiquement dès le retour du réseau.');}
  } finally {busy=false;}
}
$('fullscreen').addEventListener('click',async()=>{try{if(document.fullscreenElement)await document.exitFullscreen();else await document.documentElement.requestFullscreen();}catch{$('connection').textContent='Utilisez F11 pour le plein écran.';}});
document.addEventListener('fullscreenchange',()=>{$('fullscreen').textContent=document.fullscreenElement?'Quitter le plein écran':'Plein écran';});
$('retry').addEventListener('click',poll);
if(!/^[a-f0-9]{64}$/.test(token))state('Bienvenue dans la salle.','Ouvrez le lien de projection fourni dans les réglages du photomaton.');
else {
  const digest=await crypto.subtle.digest('SHA-256',new TextEncoder().encode(token));storageKey='colors-viewer-seen-'+Array.from(new Uint8Array(digest)).map(v=>v.toString(16).padStart(2,'0')).join('').slice(0,20);
  let seen=[];try {const value=JSON.parse(localStorage.getItem(storageKey)||'[]');if(Array.isArray(value))seen=value.filter(v=>typeof v==='string').slice(-2000);}catch{}
  queue=new ViewerQueue({seen,complete:ids=>{try{localStorage.setItem(storageKey,JSON.stringify(ids));}catch{}}});
  state('La salle se prépare.','Connexion au photomaton…');
  await poll();setInterval(poll,5000);setInterval(render,200);
}
