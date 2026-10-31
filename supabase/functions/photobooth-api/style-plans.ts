import catalog from "./styles.json" with { type: "json" };

export type FrozenShotStyle = { id: string; prompt: string; variation: string };
export const styleId = (value: unknown): value is string => typeof value === "string" && /^[a-z][a-z0-9_]{0,47}$/.test(value) && value !== "mix";

export function requestedStyles(input: Record<string, unknown>, count: number): string[] {
  const value = Object.hasOwn(input, "styles") ? input.styles : [input.style ?? "chef"];
  if (!Array.isArray(value) || value.length < 1 || value.length > count ||
      value.some(id => !styleId(id)) || new Set(value).size !== value.length ||
      (value.length > 1 && value.includes("chef")) ||
      (input.styles !== undefined && input.style !== undefined)) throw new Error("invalid_styles");
  return [...value].sort();
}

function randomInt(max: number): number {
  const ceiling = Math.floor(0x100000000 / max) * max;
  let value: number;
  do { value = crypto.getRandomValues(new Uint32Array(1))[0]; } while (value >= ceiling);
  return value % max;
}
function shuffled<T>(items: readonly T[], random = randomInt): T[] {
  const result = [...items];
  for (let i = result.length - 1; i > 0; i--) {
    const j = random(i + 1); [result[i], result[j]] = [result[j], result[i]];
  }
  return result;
}

// At most four slots, so enumerating valid unique orders is bounded by 24.
// Every selected theme appears; extra slots are assigned at random.
// Row-major print indices put repeated themes on diagonals, not adjoining cells.
export function balancedPlan(pool: readonly string[], count: number, random = randomInt): string[] {
  if (![1, 4].includes(count) || !pool.length || new Set(pool).size !== pool.length) throw new Error("invalid_plan");
  const picked = shuffled(pool, random);
  if (picked.length >= count) return picked.slice(0, count);
  const slots = Array.from({ length: count }, (_, i) => picked[i % picked.length]);
  const orders: string[][] = [];
  function visit(prefix: string[], remaining: string[]) {
    if (!remaining.length) {
      if (pool.length === 1 || (prefix[0] !== prefix[1] && prefix[0] !== prefix[2] && prefix[1] !== prefix[3] && prefix[2] !== prefix[3])) orders.push(prefix);
      return;
    }
    for (const id of new Set(remaining)) {
      const next = [...remaining]; next.splice(next.indexOf(id), 1);
      visit([...prefix, id], next);
    }
  }
  visit([], slots);
  return orders[random(orders.length)];
}

export const identityPrompt = "La photo jointe est l’unique référence des personnes. " +
  "Conserve strictement le même nombre de personnes et leur identité : mêmes visages, traits, âge apparent, carnation, cheveux, expression, pose et emplacement relatif. " +
  "Conserve exactement leur corpulence, silhouette et proportions anatomiques, ainsi que leurs aides à la mobilité. " +
  "Conserve leurs lunettes, aides auditives, prothèses et couvre-chefs personnels. Tout accessoire ajouté laisse le visage entièrement visible et les cheveux reconnaissables. " +
  "Ne rajeunis, n’amincis, n’embellis, ne caricature et ne remplace aucun visage. N’ajoute ni personne ni membre. " +
  "Les vêtements, accessoires qui ne masquent pas les visages et le décor peuvent changer. " +
  "Le thème ne doit jamais modifier l’anatomie, la peau ou les visages. Si une consigne de thème entre en conflit, la fidélité à la référence prévaut. " +
  "Garde dans le cadre toutes les parties du corps visibles dans la photo source et les mêmes distances relatives entre les personnes. " +
  "Rendu photographique, familial et bienveillant. Ne sexualise personne. Une seule image carrée, sans texte, logo, collage ni planche. " +
  "Adapte le décor autour des personnes pour remplir le carré sans rogner les personnes ou les parties du corps visibles dans la référence.";

export const creativeVariations = [
  "Invente une interprétation où l’humour vient surtout des matières et des détails des tenues, sans modifier la silhouette.",
  "Invente une interprétation où l’humour vient surtout d’un décor inattendu et de ses objets, sans changer la pose des personnes.",
  "Invente une interprétation où l’humour vient surtout d’accessoires de fête surprenants, sans couvrir ni remplacer le visage.",
  "Invente une interprétation où l’humour vient surtout d’un contraste amusant de couleurs et d’ambiance, sans modifier les visages ni les corps.",
];

export function freezePlan(choices: readonly string[], count: number): FrozenShotStyle[] {
  const pool = choices[0] === "chef" ? catalog.styles.map(s => s.id) : [...choices];
  if (pool.some(id => !catalog.styles.some(s => s.id === id))) throw new Error("invalid_styles");
  const variations = shuffled(creativeVariations);
  return balancedPlan(pool, count).map((id, index) => {
    const theme = catalog.styles.find(s => s.id === id)!;
    const variation = variations[index];
    return { id, variation,
      prompt: "Transforme cette seule photo pour un anniversaire familial. " + identityPrompt +
        " Univers choisi : " + theme.prompt +
        " Direction créative de cette pose : " + variation +
        " Choisis librement les costumes, les détails et la situation dans cet univers. Aucun costume précis n’est imposé. " +
        "Ces indications créatives restent secondaires à la fidélité des personnes."
    };
  });
}
