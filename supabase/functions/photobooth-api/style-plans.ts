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
  "Conserve strictement le même nombre de personnes et leur identité : mêmes visages, traits, âge apparent, carnation, cheveux et expression reconnaissable. " +
  "Conserve exactement leur corpulence, silhouette et proportions anatomiques, ainsi que leurs aides à la mobilité. " +
  "Conserve leurs lunettes, aides auditives, prothèses et couvre-chefs personnels. Tout accessoire ajouté laisse le visage entièrement visible et les cheveux reconnaissables. " +
  "Ne rajeunis, n’amincis, n’embellis, ne caricature et ne remplace aucun visage. N’ajoute ni personne ni membre. " +
  "Les vêtements, accessoires qui ne masquent pas les visages et le décor peuvent changer. " +
  "Le point de vue, le regard vers l’objectif et la posture peuvent évoluer doucement pour réaliser la situation du thème : assis, debout ou une interaction simple avec un objet. " +
  "Ces ajustements restent naturels, adaptés aux personnes et à leurs aides à la mobilité. Ne force aucune posture, torsion ou interaction incompatible avec la référence. " +
  "Le thème ne doit jamais modifier l’anatomie, la peau ou les visages. Si une consigne de thème entre en conflit, la fidélité à la référence prévaut. " +
  "Garde le groupe réuni, l’ordre des personnes et toutes les parties du corps visibles dans la photo source à l’intérieur du cadre. " +
  "Utilise une perspective photographique naturelle sans fisheye, sans tête grossie et sans visage trop petit. Le verre, les reflets, les accessoires et les mains ne cachent ni ne déforment aucun visage. " +
  "Rendu photographique, familial et bienveillant. Ne sexualise personne. Une seule image carrée, sans texte, logo, collage ni planche. " +
  "Adapte le décor autour des personnes pour remplir le carré sans rogner les personnes ou les parties du corps visibles dans la référence.";

export const creativeVariations = [
  "Une légère plongée révèle une scène inattendue autour du groupe qui regarde l’objectif, avec des visages grands et reconnaissables.",
  "Si les personnes et leurs aides à la mobilité le permettent, une pose assise naturelle dans un décor accueillant raconte le thème.",
  "Une interaction simple avec un objet du décor, les mains sous le menton, donne vie à la scène sans masquer aucun visage.",
  "Un angle de trois quarts doux raconte une situation de groupe dans cet univers, sans modifier les visages ni la corpulence.",
];

export function freezePlan(choices: readonly string[], count: number): FrozenShotStyle[] {
  const pool = choices[0] === "chef" ? catalog.styles.map(s => s.id) : [...choices];
  if (pool.some(id => !catalog.styles.some(s => s.id === id))) throw new Error("invalid_styles");
  const directions = new Map<string, string[]>();
  const usedDirections = new Set<string>();
  return balancedPlan(pool, count).map(id => {
    const theme = catalog.styles.find(s => s.id === id)!;
    if (!directions.has(id)) {
      const specific = (theme as { variations?: string[] }).variations;
      directions.set(id, shuffled(specific?.length === 4 ? specific : creativeVariations));
    }
    const available = directions.get(id)!;
    const next = available.findIndex(direction => !usedDirections.has(direction));
    if (next < 0) throw new Error("invalid_variations");
    const [variation] = available.splice(next, 1);
    usedDirections.add(variation);
    return { id, variation,
      prompt: "Transforme cette seule photo pour un anniversaire familial. " + identityPrompt +
        " Univers choisi : " + theme.prompt +
        " Direction créative de cette pose : " + variation +
        " Développe cette situation dans le thème choisi, avec des gestes simples et des tenues de fête adaptées. Aucun costume précis n’est imposé. " +
        "Ces indications créatives restent secondaires à la fidélité des personnes."
    };
  });
}
