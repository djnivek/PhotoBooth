# Plan de réalisation et complexité

## Évaluation

Projet **faisable**, de complexité **moyenne à élevée**. Les choix 1/4 photos, la grille
et le QR sont simples. Les risques sont la continuité live view/déclenchement/flash,
la reprise sans doublons, le temps des transformations et le vrai verrouillage Windows.
Le Canon et l’imprimante annoncés permettent une approche conventionnelle via SDK et pilote.

Estimation de travail effectif pour un développeur, après accès aux SDK, clés et matériel :

| Lot | Charge indicative | Critère de sortie |
| --- | --- | --- |
| 1. Preuve Canon + DNP + runtime | 2–4 jours | Visée, 20 prises, une vraie planche 4×6 sans erreur |
| 2. Interface tactile et séance | 3–5 jours | Les quatre combinaisons passent ; code de sortie fonctionnel |
| 3. Stockage local et API sécurisée | 3–5 jours | Reprise après coupure, isolation de deux bornes, upload idempotent |
| 4. Éditions OpenAI et file de tâches | 3–5 jours | 1/4 transformations, erreur partielle, budgets et mesure p95 |
| 5. Galerie et QR mobile | 1–2 jours | Téléchargement sur téléphone hors Wi-Fi de la borne, expiration |
| 6. Installation, impression robuste, endurance | 3–5 jours | Reboot/crash, imprimante indisponible, session prolongée |
| **Total indicatif** | **15–26 jours** | MVP utilisable et vérifié sur la borne |

Ce n’est pas un engagement calendaire. Un prototype de bout en bout peut arriver plus tôt
(environ 5–8 jours de travail avec tout disponible). Compter plutôt **3–6 semaines** pour
une version exploitable, avec du temps d’essai matériel. Délai d’accès Canon, changement
de Windows et éventuel worker externe peuvent allonger cette fourchette. Le socle livré
réduit le cadrage et le travail métier, pas les validations physiques.

## Préparé sur Mac

- Dépôt privé, cahier des charges consolidé et décisions tracées.
- Organisation et projet Supabase dédiés ; aucun autre projet modifié.
- Machine à états testable pour 1/4 prises, visée, décompte, résultats et impression explicite.
- Géométrie 4×6 et simulateur de planches, sans photo réelle.
- Interfaces caméra/imprimante pour isoler les dépendances Windows.
- Primitive de vérification du code administrateur ; intégration et persistance Windows restent à faire.
- Contrat OpenAPI cible et politique de reprise/coûts.
- Script de diagnostic Windows en lecture seule.

## Ordre conseillé sur Windows

1. Cloner le dépôt et lire `REPRISE_WINDOWS.md`. Relever version/architecture, ports,
   alimentation et pilote DNP sans modifier la configuration de DSLR Booth.
2. Obtenir EDSDK via Canon Europe et tester la visée + capture avec un programme minimal.
3. Produire une mire et une planche papier DS620. Fixer les marges et le rendu couleur.
4. Implémenter l’hôte WPF, le journal local et le mode Original complet, utilisable hors ligne.
5. Brancher Supabase : identité de borne, upload, file de tâches, stockage privé et partage.
6. Activer OpenAI avec un budget de test explicite et mesurer le rendu ; régler concurrence et qualité.
7. Ajouter galerie publique à jeton et vérifier les QR sur un téléphone via 4G/5G.
8. Tester le mode borne, l’installateur, les reprises et une simulation d’événement.

## Ce qu’il faut encore

- Accès au vrai PC, boîtier branché par USB, alimentation continue et objectif utilisé.
- Version DSLR Booth, firmware du 4000D, flash/éclairage, définition et orientation de l’écran.
- SDK Canon obtenu légalement ; pas de besoin de le publier dans le dépôt.
- DNP installée, consommables et possibilité de sacrifier quelques tirages de calibration.
- Clé API OpenAI côté serveur, crédits/limites et montant maximal de test. Ne pas mettre cette clé dans le chat ou Git.
- Choix de maintenance Windows et niveau de résistance requis aux sorties système.
- Volume attendu (séances par événement), durée de conservation et connexion disponible.

Les informations matérielles déjà confirmées ne doivent pas être redemandées.
La création de l’organisation dédiée et le démarrage Free sont déjà autorisés.
Une formule payante, un achat de certificat ou un nouvel hébergement payant doivent être chiffrés avant décision.
