# Modifier les univers sans refaire l’exécutable

La source serveur est `supabase/functions/photobooth-api/styles.json`. Le tableau `styles` donne l’ordre des boutons. Ajouter une entrée ajoute un univers ; la retirer le masque aux nouvelles séances. `prompt` contient la scène, uniquement côté serveur. La consigne d’identité commune est dans `style-plans.ts` et prévaut sur la scène.

```json
{
  "id": "ocean",
  "label": "Sous l’océan",
  "description": "Une fête sous les vagues.",
  "asset": "",
  "color": "#CFE6DB",
  "phrases": ["Les poissons préparent la fête.", "Les coraux sortent le grand jeu.", "Une vague de bonne humeur."],
  "prompt": "Un bal sous marin photographique, costumes de fête et coraux pastel. Visages entièrement visibles, anatomie et identité inchangées."
}
```

1. Modifier le fichier, incrémenter `version`, garder un identifiant stable en minuscules avec chiffres ou underscore. Ne pas réutiliser `chef` ni `mix` ; éviter aussi `original`, déjà utilisé par le mode Au naturel.
2. Exécuter les tests backend et vérifier `GET /styles`. Les libellés font au plus 48 caractères, descriptions 140, phrases 120 ; aucun tiret ni mention AI, IA ou OpenAI dans ces textes invités. Maximum 200 univers et 256 Kio de métadonnées. Choisir un fond pastel lisible avec le texte bordeaux. Si le contraste reçu est inférieur à 4,5:1, la borne utilise automatiquement le fond sauge local.
3. Ouvrir et fusionner la PR, déployer la fonction `photobooth-api` sur le seul projet `frkckrvsdsiwivdfyjau`.
4. Sur la borne : réglages → **Actualiser les univers**. Aucun redémarrage ni nouvel exécutable nécessaire. Une modification de prompt ne demande pas d’actualiser la liste.

Le serveur fournit les métadonnées publiques sans prompt, clé ou photo. Au lancement, l’app affiche immédiatement ses neuf univers embarqués et tente une seule requête en arrière-plan, limitée à six secondes, corps compris. La liste téléchargée reste en mémoire, sans fichier cache ni polling. Ouvrir le sélecteur n’appelle pas le serveur. Si une réponse arrive pendant le choix d’un invité, elle attend le retour à l’accueil. Une panne conserve la liste précédente ; cela ne garantit pas qu’une génération soit possible sans réseau.

Les assets embarqués disponibles sont `colors_party`, `halloween`, `space`, `fantasy`, `elves`, `disco`, `animals`, `showcase`, `feast` et `chef`. Un nom inconnu ou vide choisit de manière stable une des trois illustrations locales de secours. Aucun téléchargement d’image supplémentaire. Une illustration vraiment nouvelle nécessite une nouvelle version de l’app ; ajouter un univers n’en nécessite pas.

Le sélecteur défile verticalement avec Retour, La Surprise du Chef et C’est parti toujours accessibles. Il est testé avec vingt univers à 960 × 700 et 1280 × 800, y compris des textes longs et un asset inconnu. La limite serveur reste 200 entrées ; vingt est le volume couvert par les tests UI.

Pour une pose, un choix remplace le précédent. Pour quatre poses, l’invité choisit de un à quatre univers avant le départ. Quatre choix donnent une photo chacun ; trois choix donnent une photo chacun et un univers répété ; deux choix donnent deux photos chacun. Les répétitions occupent les diagonales de la planche, l’ordre ne dépend pas des clics. Un seul choix applique ce thème aux quatre photos avec quatre directions créatives différentes. La Surprise du Chef est exclusive et choisit quatre univers distincts lorsque le catalogue en contient assez. Les poses restent enchaînées sans nouveau sélecteur.

À la création serveur, `requested_styles` et `shot_styles` conservent les choix et le prompt complet de chaque pose, consigne d’identité incluse. Une modification ou un retrait de thème ne change donc pas une séance déjà créée. Les anciens champs scalaires et les anciennes séances restent pris en charge. Aucun prompt n’est envoyé par le client. Les nouvelles consignes n’affectent que les prochaines séances. Une modification de liste pendant le choix d’un invité attend son retour à l’accueil.

Les prompts demandent mêmes personnes, visages, âge, expression, cheveux, corpulence, proportions, poses, lunettes, aides auditives, prothèses, couvre-chefs et aides à la mobilité. Vêtements et décor peuvent changer. Les univers donnent une ambiance sans imposer de costume précis. Drôles de bêtes propose des costumes inspirés d’animaux en gardant les visages humains ; Sous cloche est une scène de curiosité non alimentaire ; À croquer place les invités parmi des décors de gourmandises géantes, sans les transformer en nourriture. Les lutins sont représentés avec costumes et décor géant, sans modifier les corps. C’est une consigne au modèle, pas une garantie de pixels identiques : le premier rendu réel doit être jugé par Kevin.

## Catégories du sélecteur (0.9.1)

Chaque entrée accepte `category` : `estelle` pour « by Estelle », `essentials` pour « Les incontournables », `other` pour « Autres ». Une entrée ancienne sans ce champ rejoint Autres. Les sections suivent toujours cet ordre et préservent l'ordre du tableau serveur à l'intérieur de chaque section. Les incontournables et Autres sont masqués s'ils sont vides. La section by Estelle garde une courte indication en attendant les sept thèmes des tables. Tous les neuf thèmes existants restent dans Autres en attendant le classement de Kevin. Ne pas inventer les sept noms ou leurs prompts.

Les choix peuvent traverser les catégories ; les règles de répartition équilibrée et les prompts figés par séance restent identiques. Un thème distant peut avoir `asset: ""` : une illustration locale de secours est utilisée. Ajouter ou reclasser un thème dans le JSON serveur, redéployer la fonction, puis Actualiser les univers dans les réglages suffit. Aucune migration de base ni nouvelle version Windows n'est nécessaire. Les clients antérieurs ignorent `category` et gardent leur grille plate.
