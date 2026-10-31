const origin = "https://frkckrvsdsiwivdfyjau.supabase.co";
const api = origin + "/functions/v1/photobooth-api";
// The capability remains in the fragment: never sent to the host or in a referrer.
// It is needed when a guest copies/reopens the URL. No analytics or third-party scripts.
const token = location.hash.slice(1);
// A phone may reuse an existing tab for the next QR instead of loading a new page.
addEventListener("hashchange", () => location.reload());
const status = document.querySelector("#status");
const retry = document.querySelector("#retry");
const photos = document.querySelector("#photos");
const sheet = document.querySelector("#sheet");
const archive = document.querySelector("#archive");
const expiry = document.querySelector("#expiry");
const waiting = document.querySelector("#waiting");
const waitingArt = document.querySelector("#waiting-art");
let loading = false;
let pollTimer;
let polls = 0;
let renderKey = "";

function validate(result) {
  if (![1, 4].includes(result.photo_count) || !["original", "surprise"].includes(result.mode) ||
      !Number.isFinite(Date.parse(result.expires_at)) || !Array.isArray(result.assets)) throw new Error("La galerie est incomplète. Réessayez dans un instant.");
  const assets = new Map();
  const state = result.state ?? "ready";
  if (!["ready", "processing", "failed", "delayed"].includes(state)) throw new Error("La galerie est incomplète. Réessayez dans un instant.");
  for (const asset of result.assets) {
    if (!/^((photo|original)-[1-4]\.jpg|sheet\.jpg|archive\.zip)$/.test(asset.name) || assets.has(asset.name)) throw new Error("La galerie est incomplète. Réessayez dans un instant.");
    const address = new URL(asset.url);
    if (address.origin !== origin || address.username || address.password ||
        !/^\/storage\/v1\/object\/sign\/booth-private\/[a-f0-9-]{36}\/(photo-[1-4]\.jpg|original-[1-4]\.jpg|sheet\.jpg|archive\.zip)$/.test(address.pathname) ||
        !address.searchParams.get("token")) throw new Error("Adresse photo non reconnue.");
    assets.set(asset.name, address);
  }
  const expected = ["sheet.jpg", "archive.zip"];
  for (let i = 1; i <= result.photo_count; i++) {
    expected.push(`photo-${i}.jpg`);
    if (result.mode === "surprise") expected.push(`original-${i}.jpg`);
  }
  if (state === "ready" && expected.some(name => !(result.mode === "surprise" && result.used_original_fallback && name.startsWith("photo-")) && !assets.has(name))) throw new Error("La galerie est incomplète. Réessayez dans un instant.");
  if ([...assets.keys()].some(name => !expected.includes(name))) throw new Error("La galerie est incomplète. Réessayez dans un instant.");
  return assets;
}
function download(address, filename) {
  const url = new URL(address); url.searchParams.set("download", filename); return url.href;
}
function figure(address, label, filename) {
  const item = document.createElement("figure");
  const image = document.createElement("img"); image.src = address.href; image.alt = label;
  image.loading = "lazy"; image.decoding = "async"; image.width = 1536; image.height = 1024;
  image.addEventListener("error", () => { status.textContent = "Une photo n’a pas pu se charger. Actualisez les liens pour la retrouver."; });
  const caption = document.createElement("figcaption");
  const name = document.createElement("span"); name.textContent = label;
  const link = document.createElement("a"); link.href = download(address, filename); link.textContent = "Télécharger";
  link.setAttribute("aria-label", `Télécharger ${label.toLocaleLowerCase("fr-FR")}`);
  link.rel = "noreferrer";
  caption.append(name, link); item.append(image, caption); return item;
}
function pendingFigure(label, failed) {
  const item = document.createElement("div"); item.className = "waiting-photo";
  item.textContent = failed ? `${label} indisponible pour le moment.` : `${label} en préparation…`;
  return item;
}
async function load(automatic = false) {
  if (loading) return;
  clearTimeout(pollTimer);
  loading = true; retry.disabled = true;
  if (!automatic) status.textContent = "Ouverture de vos souvenirs…";
  try {
    if (!/^[a-f0-9]{64}$/.test(token)) throw new Error("Ce lien est incomplet. Scannez le QR affiché sur le photomaton.");
    const response = await fetch(`${api}/downloads/resolve`, {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ token }), credentials: "omit", referrerPolicy: "no-referrer",
      signal: AbortSignal.timeout(15000),
    });
    if (!response.ok) throw new Error(response.status === 404 ? "Ce lien est introuvable ou a expiré." : "Le partage est momentanément indisponible. Réessayez dans un instant.");
    const result = await response.json(); const assets = validate(result);
    const state = result.state ?? "ready";
    const processing = state === "processing";
    document.body.className = processing ? "processing" : "";
    const edited = result.mode === "surprise" && !result.used_original_fallback;
    waiting.hidden = !processing;
    const art = ["colors_party", "halloween", "space", "fantasy", "elves", "disco"].includes(result.style) ? result.style : "chef";
    waitingArt.src = `assets/${art}.png`;
    const nextKey = JSON.stringify([result.photo_count, result.mode, result.used_original_fallback, state, [...assets.keys()].sort()]);
    if (!automatic || renderKey !== nextKey) {
    const sections = [];
    for (let i = 1; i <= result.photo_count; i++) {
      const section = document.createElement("section"); section.className = "pose";
      const heading = document.createElement("h2"); heading.textContent = result.photo_count === 1 ? "Votre photo" : `Pose ${i}`;
      const pair = document.createElement("div"); pair.className = edited ? "pair" : "pair single";
      const original = assets.get(result.mode === "surprise" ? `original-${i}.jpg` : `photo-${i}.jpg`);
      pair.append(original ? figure(original, `Au naturel ${i}`, `original-${i}.jpg`) : pendingFigure("Votre photo", !processing));
      if (edited) {
        const transformed = assets.get(`photo-${i}.jpg`);
        pair.append(transformed ? figure(transformed, `Métamorphose ${i}`, `metamorphose-${i}.jpg`) : pendingFigure("Votre métamorphose", !processing));
      }
      section.append(heading, pair); sections.push(section);
    }
    photos.replaceChildren(...sections);
    sheet.replaceChildren(); sheet.hidden = true; archive.hidden = true;
    if (state === "ready") {
      const sheetTitle = document.createElement("h2"); sheetTitle.textContent = "Votre planche";
      sheet.replaceChildren(sheetTitle, figure(assets.get("sheet.jpg"), "Planche 4×6", "planche-4x6.jpg")); sheet.hidden = false;
      archive.href = download(assets.get("archive.zip"), "colors-party.zip"); archive.hidden = false;
    }
    renderKey = nextKey;
    }
    expiry.textContent = `Disponibles jusqu’au ${new Date(result.expires_at).toLocaleDateString("fr-FR")}.`;
    const completed = [...assets.keys()].filter(name => /^photo-[1-4]\.jpg$/.test(name)).length;
    status.textContent = processing
      ? `Vos métamorphoses arrivent ici au fur et à mesure. Comptez environ une à deux minutes, parfois un peu plus. Vous pouvez quitter la borne et garder cette page ouverte. ${completed} sur ${result.photo_count} prête${completed > 1 ? "s" : ""}.`
      : state === "failed" ? "Une métamorphose n’a pas abouti. Les photos déjà disponibles restent ici. Prévenez la personne qui s’occupe de la borne."
      : state === "delayed" ? "La préparation prend plus de temps que prévu. Vos photos disponibles restent ici. Actualisez plus tard ou prévenez la personne qui s’occupe de la borne."
      : edited ? "Vos originaux et vos métamorphoses sont réunis ici. Gardez vos préférées, ou téléchargez le tout." : "Vos photos au naturel sont prêtes. Le téléchargement complet contient aussi votre planche.";
    retry.textContent = processing ? "Actualiser" : "Actualiser les liens";
    if (processing && polls++ < 150) pollTimer = setTimeout(() => load(true), 4000);
    else if (processing) { waiting.hidden = true; status.textContent = "La préparation prend plus de temps que prévu. Actualisez cette page dans quelques instants."; }
  } catch (error) {
    waiting.hidden = true; renderKey = "";
    document.body.className = "";
    photos.replaceChildren(); sheet.replaceChildren(); sheet.hidden = true; archive.hidden = true; expiry.textContent = "";
    status.textContent = error instanceof Error && error.name !== "TypeError" && error.name !== "TimeoutError" ? error.message : "La connexion ne répond pas. Réessayez quand vous aurez du réseau.";
    retry.textContent = "Réessayer";
  } finally { loading = false; retry.hidden = !/^[a-f0-9]{64}$/.test(token); retry.disabled = false; }
}
retry.addEventListener("click", () => { polls = 0; return load(); });
addEventListener("pagehide", () => clearTimeout(pollTimer));
load();
