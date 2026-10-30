# Modifier les univers sans refaire l’exécutable

La source serveur est `supabase/functions/photobooth-api/styles.json`. Le tableau `styles` donne l’ordre des boutons. Ajouter une entrée ajoute un univers ; la retirer le masque aux nouvelles séances. `prompt` contient la scène, uniquement côté serveur. La consigne d’identité commune est dans `cloud.ts` et prévaut sur la scène.

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

1. Modifier le fichier, incrémenter `version`, garder un identifiant stable en minuscules avec chiffres ou underscore. Ne pas réutiliser `chef`.
2. Exécuter les tests backend et vérifier `GET /styles`. Les libellés font au plus 48 caractères, descriptions 140, phrases 120 ; aucun tiret ni mention AI, IA ou OpenAI dans ces textes invités. Maximum 200 univers et 256 Kio de métadonnées. Choisir un fond pastel lisible avec le texte bordeaux. Si le contraste reçu est inférieur à 4,5:1, la borne utilise automatiquement le fond sauge local.
3. Ouvrir et fusionner la PR, déployer la fonction `photobooth-api` sur le seul projet `frkckrvsdsiwivdfyjau`.
4. Sur la borne : réglages → **Actualiser les univers**. Aucun redémarrage ni nouvel exécutable nécessaire. Une modification de prompt ne demande pas d’actualiser la liste.

Le serveur fournit les métadonnées publiques sans prompt, clé ou photo. Au lancement, l’app affiche immédiatement ses six univers embarqués et tente une seule requête en arrière-plan, limitée à six secondes, corps compris. La liste téléchargée reste en mémoire, sans fichier cache ni polling. Ouvrir le sélecteur n’appelle pas le serveur. Si une réponse arrive pendant le choix d’un invité, elle attend le retour à l’accueil. Une panne conserve la liste précédente ; cela ne garantit pas qu’une génération soit possible sans réseau.

Les assets embarqués disponibles sont `colors_party`, `halloween`, `space`, `fantasy`, `elves`, `disco` et `chef`. Un nom inconnu ou vide choisit de manière stable une des trois illustrations locales de secours. Aucun téléchargement d’image supplémentaire. Une illustration vraiment nouvelle nécessite une nouvelle version de l’app ; ajouter un univers n’en nécessite pas.

La Surprise du Chef tire un seul univers pour les quatre poses. À la création de la séance serveur, `requested_style`, `style` et la scène `style_prompt` sont conservés. Modifier ou retirer cette scène n’affecte pas les poses restantes de cette séance. La consigne d’identité commune reste dans le code serveur ; éviter de la modifier pendant une séance en cours.

Les prompts demandent mêmes personnes, visages, âge, expression, cheveux, corpulence, proportions, poses et aides à la mobilité. Vêtements et décor peuvent changer. Les lutins sont représentés avec costumes et décor géant, sans modifier les corps. C’est une consigne au modèle, pas une garantie de pixels identiques : le premier rendu réel doit être jugé par Kevin.
