const api = "https://frkckrvsdsiwivdfyjau.supabase.co/functions/v1/photobooth-api";
const token = location.hash.slice(1);
const status = document.querySelector("#status");
const retry = document.querySelector("#retry");
const photos = document.querySelector("#photos");
const archive = document.querySelector("#archive");
let loading = false;

async function load() {
  if (loading) return;
  loading = true; retry.disabled = true;
  status.textContent = "Ouverture de vos souvenirs…";
  try {
    if (!/^[a-f0-9]{64}$/.test(token)) throw new Error("Ce lien est incomplet. Scannez le QR affiché sur le photomaton.");
    const response = await fetch(`${api}/downloads/resolve`, {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ token }), credentials: "omit", referrerPolicy: "no-referrer",
    });
    if (!response.ok) throw new Error(response.status === 404 ? "Ce lien est introuvable ou a expiré." : "Le téléchargement est momentanément indisponible. Réessayez dans un instant.");
    const result = await response.json();
    photos.replaceChildren(); archive.hidden = true;
    for (const asset of result.assets.sort((a,b) => a.name.localeCompare(b.name))) {
      const address = new URL(asset.url);
      if (address.origin !== "https://frkckrvsdsiwivdfyjau.supabase.co") throw new Error("Adresse photo non reconnue.");
      if (asset.name === "archive.zip") { archive.href = address.href; archive.hidden = false; continue; }
      if (!/^((photo|original)-[1-4]\.jpg|sheet\.jpg)$/.test(asset.name)) continue;
      const sheet = asset.name === "sheet.jpg";
      const name = sheet ? "Votre planche 4×6" : (asset.name.startsWith("original-") ? `Original ${asset.name[9]}` : `Photo ${asset.name[6]}`);
      const figure = document.createElement("figure"); if (sheet) figure.className = "sheet";
      const image = document.createElement("img"); image.src = address.href; image.alt = name; image.loading = "lazy";
      const caption = document.createElement("figcaption");
      const label = document.createElement("span"); label.textContent = name;
      const link = document.createElement("a"); link.href = address.href; link.textContent = "Ouvrir la photo"; link.target = "_blank"; link.rel = "noopener noreferrer";
      caption.append(label,link); figure.append(image,caption); photos.append(figure);
    }
    document.querySelector("#expiry").textContent = `Disponibles jusqu’au ${new Date(result.expires_at).toLocaleDateString("fr-FR")}.`;
    status.textContent = "Vos photos sont prêtes. Le téléchargement complet contient aussi la planche imprimable.";
  } catch (error) { status.textContent = error instanceof Error ? error.message : "Impossible de charger les photos."; }
  finally { loading = false; retry.hidden = false; retry.disabled = false; }
}
retry.addEventListener("click",load);
load();
