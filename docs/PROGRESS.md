# Progression — anniversaire d’Estelle

Objectif : usage personnel lors de l’anniversaire du 31 octobre 2026. Priorité à un
parcours complet et vérifiable dès le PC de test sans Canon ni imprimante. La diffusion
commerciale, la flotte de bornes et la maintenance longue durée ne sont pas le périmètre.

## Livraisons

| Lot | État | Critère |
| --- | --- | --- |
| Préparation initiale | Livré sur main, `169c66c` | Documents, 23 tests métier, 3 tests health, projet Supabase |
| [PR 1](https://github.com/djnivek/PhotoBooth/pull/1) — périmètre et handoff | Fusionnée, `c42162f` | Plan anniversaire, portes de validation claires |
| [PR 2](https://github.com/djnivek/PhotoBooth/pull/2) — traitement image et fichiers | Fusionnée | Orientation, grille, JPEG final unique, persistance et tests pixels |
| [PR 3](https://github.com/djnivek/PhotoBooth/pull/3) — interface et simulation | Fusionnée | Parcours tactile animé, mode simulé sans appel payant, aperçu imprimable |
| [PR 4](https://github.com/djnivek/PhotoBooth/pull/4) — adaptateurs Windows | Fusionnée ; CI Windows réussie, matériel non testé | Canon EDSDK, GDI DNP, journal anti-doublon |
| [PR 5](https://github.com/djnivek/PhotoBooth/pull/5) — API et partage | Fusionnée ; backend déployé, tests Mac et CI Windows passés | Originaux + résultats privés, OpenAI asynchrone, QR ZIP |
| [PR 6](https://github.com/djnivek/PhotoBooth/pull/6) — Canon sans SDK | Fusionnée, tests Mac et CI Windows passés ; matériel non testé | WPD/PTP avec pilote Windows existant |
| [PR 7](https://github.com/djnivek/PhotoBooth/pull/7) — paquet Windows | Fusionnée, `c4ba833` ; [release 0.2.0](https://github.com/djnivek/PhotoBooth/releases/tag/v0.2.0) publiée | ZIP extrait et exécuté sur Windows, quatre modes simulés, cadrage natif corrigé et revu |
| [PR 8](https://github.com/djnivek/PhotoBooth/pull/8) — preuves et simulateur HTTP | Fusionnée, `65d40e2` ; CI réussie | Preuves Windows durables et 32 séances fictives concurrentes |
| [PR 9](https://github.com/djnivek/PhotoBooth/pull/9) — correctifs 0.2.1 | Fusionnée, `2e02633` ; [release0.2.1 publiée](https://github.com/djnivek/PhotoBooth/releases/tag/v0.2.1), revue ciblée et CI terminées | Accès fichiers/PIN, impression interséances, annulation USB et deuxième séance : quatre régressions corrigées |
| [PR 10](https://github.com/djnivek/PhotoBooth/pull/10) — handoff final 0.2.1 | Reçus et captures versionnés ; release publiée | Source exacte, SHA retéléchargé, revue et checklist PC/matériel |

Chaque livraison passe par une PR, les vérifications applicables, puis un merge.
Le tableau doit évoluer avec les numéros et liens réels, sans appeler « fait » un simple contrat.
Le [reçu Windows](evidence/windows-0.2.1/README.md) conserve le commit, le SHA et les captures.
Les PR 3/5/6/7/8/9 contiennent désormais des screenshots ; poursuivre cette pratique à chaque modification UI.

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

Revue ciblée **terminée** avec Claude CLI Opus 5.5, effort max, sur `65d40e2..496fff1`
et un brief d’architecture/preuves. Verdict : version de répétition livrable, aucun blocage
établi dans le diff. Les deux tentatives d’audit plus large n’ont pas produit de rapport ;
ne pas présenter cette revue comme un audit intégral du dépôt. Aucun secret ni photo personnelle.
[Rapport, reçu et traitement des remarques](reviews/2026-10-30-final-review.md).
Les tests complémentaires couvrent le nettoyage USB après annulation, l’erreur pilote tardive,
le code lors de la fermeture et l’impression après retour explicite aux originaux.

## Éléments encore nécessaires

Essai Canon WPD (SDK officiel optionnel absent), clé OpenAI côté serveur, code de sortie choisi par Kevin,
sélection de l’imprimante sur le photomaton. Les fichiers de simulation ne doivent jamais être
confondus avec une vraie capture Canon ou une vraie image générée.
