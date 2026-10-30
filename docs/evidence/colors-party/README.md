# Colors Party 0.3.0 : preuves

Les captures avant sont celles de main `cf93936256c20a21510e487341469bb5d8aefe0e`, version0.2.2. Les captures après proviennent de la suite Avalonia native headless exécutée sur Mac, avec les mêmes contraintes de fenêtres. Ce sont des fixtures de test, pas des photos d’invités ni des résultats OpenAI.

| État | Avant | Après |
| --- | --- | --- |
| Accueil 1280×800 | [Avant](before/home.png) | [Après](../../../.impeccable/review/desktop-1280x800.png) |
| Choix créatif | [Avant](before/choice.png) | [Six univers](../../../.impeccable/review/surprise-choice.png) |
| Petit écran | | [960×700](../../../.impeccable/review/styles-960x700.png) |
| Attente Halloween | | [Animation capturée](../../../.impeccable/review/processing-halloween.png) |
| Catalogue actualisé | | [Nouvel univers et fond sûr](../../../.impeccable/review/dynamic-styles.png) |
| Réglages | | [Code et plein écran](../../../.impeccable/review/settings-bottom.png) |

Tests Release locaux : 26 métier, 89 image, 50 runtime, 26 impression, 28 séquence Canon, 46 cloud, parcours UI, 11 backend et 5 galerie. Aucun accès matériel ou appel OpenAI dans ces tests. OpenAPI3.0.3 validé. Les huit PNG de produit conservent leur prompt exact et leur provenance intégrée.

[Critique Claude du brief](../../reviews/colors-party/claude-styles.md) et [revue Impeccable](../../reviews/colors-party/impeccable.md). [Supabase déployé et vérifié](backend.json) : migration appliquée, huit contrôles de catalogue/auth/séance, aucune photo envoyée ni génération. Clé OpenAI encore absente. Les reçus PR, CI Windows et SHA de l’archive seront ajoutés après les vérifications correspondantes. Ne pas confondre cette validation locale avec un test sur le photomaton Windows10.
