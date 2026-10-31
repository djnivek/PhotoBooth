# Les thèmes de la soirée

Le plan de table fourni par Kevin contient huit thèmes, Les Marmots compris. Seuls les noms des tables ont été repris ; la photo du plan et les noms des invités ne sont pas intégrés au dépôt ni transmis pour générer les illustrations. « Le Velour » reprend le libellé imprimé sur le plan.

| by Estelle | Univers photographique | Illustration et prompt de création |
| --- | --- | --- |
| Le Saphir | Palais de saphirs, géodes, terrasse de cristal bleu | [sapphire.png](../src/PhotoBooth.App/Assets/Party/sapphire.png), [prompt](../src/PhotoBooth.App/Assets/Party/sapphire.prompt.txt) |
| La Lavande | Jardin provençal, bouquets géants et banc de jardin | [lavender.png](../src/PhotoBooth.App/Assets/Party/lavender.png), [prompt](../src/PhotoBooth.App/Assets/Party/lavender.prompt.txt) |
| Le Cachemire | Cocon textile, coussins et plis de cachemire paisley | [cashmere.png](../src/PhotoBooth.App/Assets/Party/cashmere.png), [prompt](../src/PhotoBooth.App/Assets/Party/cashmere.prompt.txt) |
| L’Olivier | Jardin méditerranéen, branches argentées et table de réception | [olive.png](../src/PhotoBooth.App/Assets/Party/olive.png), [prompt](../src/PhotoBooth.App/Assets/Party/olive.prompt.txt) |
| Le Citron | Citronniers, limonade et banquette sous une arche jaune | [lemon.png](../src/PhotoBooth.App/Assets/Party/lemon.png), [prompt](../src/PhotoBooth.App/Assets/Party/lemon.prompt.txt) |
| Le Velour | Rideaux, loge de théâtre et grand canapé rouge | [velvet.png](../src/PhotoBooth.App/Assets/Party/velvet.png), [prompt](../src/PhotoBooth.App/Assets/Party/velvet.prompt.txt) |
| Le Sunset | Terrasse à l’heure dorée, horizon marin et ciel corail | [sunset.png](../src/PhotoBooth.App/Assets/Party/sunset.png), [prompt](../src/PhotoBooth.App/Assets/Party/sunset.prompt.txt) |
| Les Marmots | Jouets démesurés, petit train et cubes de jeu, sans changer l’âge des personnes | [marmots.png](../src/PhotoBooth.App/Assets/Party/marmots.png), [prompt](../src/PhotoBooth.App/Assets/Party/marmots.prompt.txt) |

**Les incontournables**, dans cet ordre : Colors Party, Halloween, Sous cloche.

**Autres**, dans cet ordre : Dans les étoiles, Pays imaginaire, Petits lutins, Disco velours, Drôles de bêtes, À croquer.

**La Surprise du Chef** reste un choix séparé et exclusif, toujours dans le pied du sélecteur. Elle peut choisir parmi les dix sept univers. La sélection d’un à quatre thèmes sur quatre poses conserve sa répartition équilibrée, avec répétitions en diagonale.

## Situations et fidélité

La demande actuelle autorise des changements mesurés de point de vue, de regard et de posture. Elle remplace la contrainte antérieure de garder exactement la pose. L’identité, le nombre de personnes, l’âge, les cheveux, la corpulence, les proportions et les aides à la mobilité restent prioritaires. Les gestes doivent être simples et anatomiquement crédibles ; les personnes restent dans le même ordre et les visages lisibles, sans verre, reflet, main ou accessoire devant eux.

Colors Party utilise une plongée oblique comme depuis un drone bas, avec les regards vers l’objectif et beaucoup de couleurs dans le décor. La caméra reste assez proche et emploie une perspective naturelle. Sous cloche place vraiment le groupe dans une cloche spacieuse dont la base, les côtés et le sommet restent lisibles, avec des paumes contre la paroi intérieure sous les mentons ou sur les côtés.

Chaque univers fournit quatre directions privées dans [styles.json](../supabase/functions/photobooth-api/styles.json). La consigne commune et la sélection des directions sont dans [style-plans.ts](../supabase/functions/photobooth-api/style-plans.ts). Quatre photos d’un même thème utilisent quatre directions distinctes ; les prompts complets sont figés pour la séance. Aucun prompt ni direction n’est envoyé à la galerie publique. Le modèle reste Sunburst direct, qualité medium, 1024×1024, une édition par photo et la concurrence existante.

Les huit PNG transparents ont été créés avec l’outil intégré de génération d’images, à partir du style gouache approuvé `colors_party.png`. Ils sont embarqués dans la borne et les ressources d’impression, sans téléchargement 4G par séance. Les `.prompt.txt` adjacents conservent les prompts exacts des illustrations. La composition de planche et le papier restent identiques.

Les tests automatiques vérifient les ressources, l’alpha, les catégories, les plans équilibrés, les directions figées, les interfaces et la composition d’impression. Ils ne prouvent pas la fidélité d’un vrai portrait après changement de perspective : Kevin doit juger un rendu réel avant l’utilisation en soirée.

Référence de formulation : [guide officiel OpenAI de prompting d’images](https://developers.openai.com/api/docs/guides/image-prompting), notamment la séparation des changements et invariants, l’explicitation du regard et de l’interaction avec la scène, et l’inspection du résultat. Cette consultation ne change aucun réglage d’API ni le modèle demandé.
