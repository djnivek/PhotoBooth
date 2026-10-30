# Progression — anniversaire d’Estelle

Objectif : usage personnel lors de l’anniversaire du 31 octobre 2026. Priorité à un
parcours complet et vérifiable dès le PC de test sans Canon ni imprimante. La diffusion
commerciale, la flotte de bornes et la maintenance longue durée ne sont pas le périmètre.

## Livraisons

| Lot | État | Critère |
| --- | --- | --- |
| Préparation initiale | Livré sur main, `169c66c` | Documents, 23 tests métier, 3 tests health, projet Supabase |
| PR 1 — périmètre et handoff | En cours | Plan anniversaire, portes de validation claires |
| PR 2 — traitement image et fichiers | À faire | Orientation, grille, JPEG final unique, persistance et tests pixels |
| PR 3 — interface et simulation | À faire | Parcours tactile animé, mode simulé sans appel payant, aperçu imprimable |
| PR 4 — intégrations et paquet Windows | À faire | Canon, DNP, API réelle sur action explicite, QR, ZIP autonome, handoff |

Chaque livraison passe par une PR, les vérifications applicables, puis un merge.
Le tableau doit évoluer avec les numéros et liens réels, sans appeler « fait » un simple contrat.

## Portes de validation

- **Mac maintenant** : UI native, séquences, images synthétiques/locales, orientation réelle,
  montage, fichiers finaux, sécurité des reprises, requêtes API simulées, packaging croisé.
- **PC ce soir** : lancer le paquet téléchargé sans SDK, tester UI, fichiers simulés, dialogue
  de réglage, appel OpenAI uniquement sur décision de Kevin et QR depuis un téléphone.
- **Photomaton demain** : connexion et live view Canon, flash/capture, pilote DS620, vrai papier,
  impression et marge/couleur. L’utilisateur dispose uniquement du téléchargement du paquet.

## Contraintes confirmées

- Canon EOS 4000D, DNP DS620, 4×6, Windows 10 Pro 22H2 ; ce matériel n’est pas branché aujourd’hui.
- 1 ou 4 prises, Original ou IA aléatoire, qualité **xhigh** ; jamais de test payant automatique.
- Plein écran protégé par code ; limites des raccourcis système Windows acceptées comme sujet terrain.
- Même JPEG de planche pour aperçu, export et impression ; aucun montage différent dans le pilote.
- Pas de nouvelle génération ni double impression lors d’une reprise ambiguë.
- Un PC de test sans caméra ni imprimante doit pouvoir simuler tout le parcours et montrer
  ce qui serait envoyé à l’impression ; ne jamais annoncer qu’une feuille a été imprimée.
- Revue d’architecture demandée à Claude CLI **Opus 5.5, effort max** ; statut réel ci-dessous.

## Revue Claude

CLI authentifié vérifié. Le contrôle automatique a refusé l’envoi initial du brief technique ;
autorisation explicite de transmission sans secrets demandée à Kevin. Aucun brief envoyé à ce stade.

## Éléments encore nécessaires

SDK Canon licencié (absent du dépôt), clé OpenAI côté serveur, code de sortie choisi par Kevin,
sélection de l’imprimante sur le photomaton. Les fichiers de simulation ne doivent jamais être
confondus avec une vraie capture Canon ou une vraie image générée.
