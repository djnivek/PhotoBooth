const origin = "https://frkckrvsdsiwivdfyjau.supabase.co";
const api = origin + "/functions/v1/photobooth-api";
// The capability remains in the fragment: never sent to the host or in a referrer.
// It is needed when a guest copies/reopens the URL. No analytics or third-party scripts.
const token = location.hash.slice(1);
const status = document.querySelector("#status");
const retry = document.querySelector("#retry");
const photos = document.querySelector("#photos");
const sheet = document.querySelector("#sheet");
const archive = document.querySelector("#archive");
const expiry = document.querySelector("#expiry");
let loading = false;

function validate(result) {
  if (![1, 4].includes(result.photo_count) || !["original", "surprise"].includes(result.mode) ||
      !Number.isFinite(Date.parse(result.expires_at)) || !Array.isArray(result.assets)) throw new Error("La galerie est incomplète. Réessayez dans un instant.");
  const assets = new Map();
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
  if (expected.some(name => !assets.has(name))) throw new Error("La galerie est incomplète. Réessayez dans un instant.");
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
async function load() {
  if (loading) return;
  loading = true; retry.disabled = true;
  status.textContent = "Ouverture de vos souvenirs…";
  try {
    if (!/^[a-f0-9]{64}$/.test(token)) throw new Error("Ce lien est incomplet. Scannez le QR affiché sur le photomaton.");
    const response = await fetch(`${api}/downloads/resolve`, {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ token }), credentials: "omit", referrerPolicy: "no-referrer",
      signal: AbortSignal.timeout(15000),
    });
    if (!response.ok) throw new Error(response.status === 404 ? "Ce lien est introuvable ou a expiré." : "Le partage est momentanément indisponible. Réessayez dans un instant.");
    const result = await response.json(); const assets = validate(result);
    const sections = [];
    const edited = result.mode === "surprise" && !result.used_original_fallback;
    for (let i = 1; i <= result.photo_count; i++) {
      const section = document.createElement("section"); section.className = "pose";
      const heading = document.createElement("h2"); heading.textContent = result.photo_count === 1 ? "Votre photo" : `Pose ${i}`;
      const pair = document.createElement("div"); pair.className = edited ? "pair" : "pair single";
      pair.append(figure(assets.get(edited ? `original-${i}.jpg` : `photo-${i}.jpg`), `Au naturel ${i}`, `original-${i}.jpg`));
      if (edited) pair.append(figure(assets.get(`photo-${i}.jpg`), `Métamorphose ${i}`, `metamorphose-${i}.jpg`));
      section.append(heading, pair); sections.push(section);
    }
    photos.replaceChildren(...sections);
    const sheetTitle = document.createElement("h2"); sheetTitle.textContent = "Votre planche";
    sheet.replaceChildren(sheetTitle, figure(assets.get("sheet.jpg"), "Planche 4×6", "planche-4x6.jpg")); sheet.hidden = false;
    archive.href = download(assets.get("archive.zip"), "colors-party.zip"); archive.hidden = false;
    expiry.textContent = `Disponibles jusqu’au ${new Date(result.expires_at).toLocaleDateString("fr-FR")}.`;
    status.textContent = edited ? "Vos originaux et vos métamorphoses sont réunis ici. Gardez vos préférées, ou téléchargez le tout." : "Vos photos au naturel sont prêtes. Le téléchargement complet contient aussi votre planche.";
    retry.textContent = "Actualiser les liens";
  } catch (error) {
    photos.replaceChildren(); sheet.replaceChildren(); sheet.hidden = true; archive.hidden = true; expiry.textContent = "";
    status.textContent = error instanceof Error && error.name !== "TypeError" && error.name !== "TimeoutError" ? error.message : "La connexion ne répond pas. Réessayez quand vous aurez du réseau.";
    retry.textContent = "Réessayer";
  } finally { loading = false; retry.hidden = !/^[a-f0-9]{64}$/.test(token); retry.disabled = false; }
}
retry.addEventListener("click", load);
load();
