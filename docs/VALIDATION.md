# Validation

## Version 0.6 — validation locale

437 assertions C# réussies (métier 26, imaging 231, runtime 54, impression 26, Canon fictif 28, cloud 72), parcours UI étendu passé ; 22 tests Deno, typecheck/lint, 18 tests galerie et OpenAPI réussis. Les tests client bloquent les résultats jusqu’aux huit départs de deux séances. Les tests serveur vérifient le 202 immédiat, le travail en arrière-plan, l'absence de nouveau POST Responses, les erreurs ambiguës sans relance, la récupération d'une sortie persistée et la lecture des anciens jobs Responses.

Quatorze planches de calibration (sept thèmes × une/quatre poses) sont en 1800 × 1200, 300 dpi ; bords, ordre, dimensions, cadrage sans superposition et octets Au naturel inchangés vérifiés. Revue indépendante : ship pour les thèmes intégrés. Le ZIP extrait passe les quatre parcours simulés et l'ouverture native à 1280 × 800 et 960 × 700. Pas de DNP physique, pas de portrait IA, pas de benchmark 4G. Aperçus et [reçu local](evidence/direct-square/delivery.json) versionnés dans [direct-square](evidence/direct-square/README.md). Statut distant consigné dans HANDOFF.


## Historique 0.5.0 — 31 octobre 2026

Paquet autonome extrait et exécuté sur ce PC Windows 11 Pro x64, build26300. Pas de Canon ni de DNP connectés. Les HTTP OpenAI restent fictifs dans les tests et aucune génération payante n’a été déclenchée. La 0.5 est un paquet local avec backend déployé, pas encore une release GitHub ou une CI publiée.

| Vérification | Résultat courant |
| --- | --- |
| C# Release | 285 assertions : métier26, imaging94, runtime50, impression26, Canon fictif28, cloud61 |
| Quatre générations parallèles | Faux HTTP bloqué jusqu’aux quatre départs ; résultats exacts, copies d’entrée privées, huit uploads avant publication puis seulement la planche, publication rejouée sans upload supplémentaire |
| Images | Entrée JPEG90 bornée à 1536px, orientation/cadrage et bords conservés, pas d’agrandissement, originaux inchangés ; résultat direct vérifié par SHA sans réencodage |
| UI native | Parcours étendu : QR anticipé, attente visible, séance suivante pendant traitement précédent, retry absent après succès, bouton principal visible |
| API Deno | 19 tests, typecheck et lint : ZIP serveur, résultat direct, copies privées, 429 sans place bloquée, protections et absence de relance payante ambiguë |
| Galerie | 18 contrats et 10 cas Chromium synthétiques ; pas de modification de l’interface entre 0.4 et 0.5 |
| OpenAPI | Contrat0.5 validé avec swagger-cli |
| Supabase réel | 47 contrôles sans génération : QR stable anticipé, progression, entrées privées, dix assets, neuf entrées exactes du ZIP serveur, publication répétable |
| Production navigateur | Chromium 1280×900 et 390×844, vrais Vercel/Supabase, photos synthétiques, aucun appel fournisseur |
| Paquet0.5 | SHA256 `2becf3afdacf3c05eb015fcff6bceca74c76e49141f3cdce2c94c6c90247e78f` ; quatre modes self-test et fenêtres natives1280×800/960×700 réussis après extraction |
| Protection distante | Migration d’entrées optimisées appliquée, aucun SELECT anon/authenticated sur les assets, audit sécurité sans alerte ; compteur de générations resté à1 avant/après |

Reçus ignorés par Git dans `artifacts/optimization-0.5.0/` : `delivery.json`, `transfer-benchmark.json`, `live-smoke/{receipt,browser}.json`, `package-checks/` et journaux C#. Revue de l’interface : `artifacts/progressive-0.4.0/review.json`. La correction mobile a été vérifiée après revue Impeccable.

Le benchmark emploie quatre images synthétiques détaillées, pas des portraits ni des résultats IA. Copies d’entrée : −84,3 %. Estimation du volume total montant selon les deux circuits : −58,9 % (24,22→9,95 Mo). Préparation locale mesurée à341ms. Aucune durée réelle de génération/4G ou fidélité de visage n’en découle. Sortie1536×1024 et Sunburst/xhigh conservés. Les comparaisons de profil rapide restent proposées, sans activation.

## Historique — 30 octobre 2026

**Application et API codées ; simulation native et partage distant vérifiés. Aucun Canon,
aucune feuille DNP, aucun lancement sur le Windows 10 cible, aucune édition OpenAI facturée.**

| Vérification | Résultat |
| --- | --- |
| Solution Release .NET10 | 0 avertissement, 0 erreur sur Mac arm64 |
| Métier | 26 assertions : quatre parcours, séquences, résultats, PIN, impression explicite |
| Imaging | 89 assertions : huit EXIF réels, pixels, ordre, montage, DPI, fichiers et SHA |
| Runtime | 50 assertions : cycle complet, persistance, annulation, connexion USB tardive sans chevauchement ni fermeture prématurée, erreur COM après annulation |
| Préparation Windows | 26 assertions : BGRA, axes DPI distincts, zone imprimable, journal avant effet externe et suivi d’impression après changement de séance |
| Canon WPD | 28 assertions avec transport fictif : séquence visée/capture, quatre vrais fichiers, RAW+JPEG, timeout, aucun redéclenchement |
| Client cloud | 47 assertions : quatre éditions ordonnées, originaux envoyés une fois, ZIP 5/9 fichiers, reprise sans facturation supplémentaire, fallback explicitement imprimable, credentials et 32 séances HTTP fictives concurrentes |
| UI native rendue headless | Accueil1280/1024/960, deux séances complètes successives, annulation de connexion lente et erreur COM tardive, export impression, fermeture et accès aux fichiers protégés par code erroné/annulé/valide |
| API Deno | 14 tests, typecheck et lint : auth, idempotence, réservation, erreurs ambiguës, originaux requis, hash et expiration |
| Galerie | 14 tests DOM ; Chromium ordinateur/mobile, 1/4 originales/modifiées, récupération réseau, expiration, nouveau QR dans le même onglet, captures avant/après |
| OpenAPI3.0.3 | Validation formelle réussie avec openapi-spec-validator0.7.2 |
| Colors Party 0.3.1 Windows | [Run41139279362](https://github.com/djnivek/PhotoBooth/actions/runs/41139279362) réussi sur main 82766b0 : suite complète, Chromium ordinateur/mobile, ZIP extrait/exécuté, quatre parcours, fenêtres natives. Release retéléchargée avec SHA identique ; [preuves](evidence/offline-gallery/README.md) |
| Colors Party0.3.0 Windows | [Run41131796018](https://github.com/djnivek/PhotoBooth/actions/runs/41131796018) réussi sur main d217c00 : suite complète, ZIP extrait, quatre parcours, fenêtre réelle 992×700 puis960×700, zéro appel OpenAI ; [preuves](evidence/colors-party/README.md) |
| CI Windows initiale | [Run41088358348](https://github.com/djnivek/PhotoBooth/actions/runs/41088358348) réussi : PR4, Windows Server 2022, tests natifs headless et simulation |
| CI Windows cloud | [Run41096255110](https://github.com/djnivek/PhotoBooth/actions/runs/41096255110) réussi sur le commit de PR5 : client, DPAPI, UI et API simulées |
| CI Windows Canon | [Run41098262975](https://github.com/djnivek/PhotoBooth/actions/runs/41098262975) réussi sur PR6 : adaptateur fictif et application native |
| EXE Windows extrait | [Run41105339893](https://github.com/djnivek/PhotoBooth/actions/runs/41105339893) réussi sur main `c4ba833` : paquet autonome extrait, quatre parcours, raster, archives2/3/5/9 fichiers, QR, DPAPI |
| Correctifs 0.2.1 sur Windows | [Run41116066419](https://github.com/djnivek/PhotoBooth/actions/runs/41116066419) réussi sur PR9 `365ceca` : quatre régressions et compléments de revue, quatre parcours de l’EXE extrait et fenêtre native |
| Release Windows 0.2.1 | [Run41116597603](https://github.com/djnivek/PhotoBooth/actions/runs/41116597603) réussi sur main `2e02633` ; paquet publié, retéléchargé et SHA vérifié ; [reçus](evidence/windows-0.2.1/README.md) |
| Fenêtre Windows réelle | Pixels bureau et rendu natif à 992×700, puis 960×700 après resize : Réglages, Démarrer et planche visibles dans leurs panneaux ; revue Impeccable acceptée à échelle 1 |
| Supabase réel | ZIP 5 fichiers puis ZIP 9 fichiers (4 originaux + 4 effets locaux + planche), octets vérifiés, publication rejouée, 0 génération |
| Isolation distante | Quatre tables RLS activées, aucun SELECT anon/authenticated ; bucket privé ; budget started=0 relu |
| Galerie hébergée | Oui : Vercel, fichiers déployés comparés, dix assets signés vérifiés, download individuel et CORS validés. Voir [les reçus0.3.1](evidence/offline-gallery/README.md) |

## Colors Party 0.3.0

Validation Release complète sur Mac terminée le 30 octobre : 0 avertissement, 0 erreur, contrôles du tableau actualisés ci-dessus. Le test UI exécute la boucle de dispatcher, donc les vrais timers, avec souris et pointeur tactile headless. Il couvre maintien court, sortie du coin, trois secondes, code erroné/correct/absent, plein écran gardant les réglages, retour des univers sans appel supplémentaire, ajout/retrait, panne réseau, réponse initiale différée et repli de contraste d’une couleur serveur volontairement illisible. Le test cloud inclut la taille maximale et l’annulation pendant le corps HTTP.

Les images de géométrie sont des fixtures. Les tests ne génèrent aucune image distante et ne prouvent ni une identité conservée par OpenAI, ni l’autofocus du 4000D, ni une feuille imprimée. Les 28 contrôles Canon couvrent la séquence et le refus d’un déclenchement sans préparation réussie, via transport injecté.

Mémoire et animation bornées par construction : wordmark décodé à 800px, sept emblèmes à 288px une seule fois par fenêtre ; pas de canvas plein écran ou de particules. Un frame vidéo en attente de rendu, traitement des grandes images hors thread UI, timer de déplacement à 12,5Hz, cycle 2,5s, arrêt hors traitement. Ce sont des limites de conception, pas un benchmark de l’ancien i7.

Les rendus headless Avalonia/Skia sont versionnés dans `.impeccable/review/`.
Les captures du véritable exécutable Windows sont dans [windows-0.2.1](evidence/windows-0.2.1/README.md) :
Windows Server 2022 sur runner GitHub, pas le photomaton Windows 10. La revue Impeccable reste limitée
aux états observés. Le navigateur local reste désactivé dans Work. En0.3.1, Chromium en CI Windows a produit des captures ordinateur/mobile réellement inspectées et une revue indépendante, avec API et images synthétiques. Le QR scanné sur un vrai téléphone reste une recette distincte.

## Hors ligne et protection0.3.1

Cloud activé avec HTTP en échec : une/quatre poses Au naturel, planche locale et octets d’impression identiques. Aucun QR fictif ni appel de génération. SQL transactionnel annulé : plafond, cadence, travaux simultanés, arrêt, droits/RLS et bucket privé vérifiés ; compteur resté à zéro. Les refus d’accès API ont aussi été contrôlés sur le vrai serveur. [Avant/après et limites](evidence/offline-gallery/README.md).

## Reproduire

```sh
AVALONIA_TELEMETRY_OPTOUT=1 sh scripts/verify.sh
# Windows : ./scripts/verify.ps1
```

Sur ce Mac : SDK10.0.403 dans `/tmp/photobooth-dotnet`, runtime10.0.14, Deno2.9.2, Node22.
`PHOTOBOOTH_DOTNET` permet d’indiquer le chemin du SDK temporaire. Les tests sont des exécutables
C# d’assertions ; les HTTP payants sont remplacés, et les tests Deno n’ont pas de permission réseau.

Le test distant volontaire `tools/PhotoBooth.CloudSmoke` utilise uniquement des mires et un
filtre local, jamais `BoothCloudClient.TransformAsync` ni une route de génération. Les reçus
sanitisés dans [evidence](evidence/) ne contiennent ni token QR, ni mot de passe, ni clé.

## À prouver sur les machines

- Windows 10 Pro 22H2 exact (distinct du lancement déjà réussi sur Server 2022).
- Liaison caméra, version/runtime Canon retenu, vraie live view, autofocus/flash et transfert.
- Pilote DS620, paramètres 4×6, feuille réelle, marge et couleur.
- Un appel OpenAI manuel puis quatre : modèle disponible, résultat, temps et coût mesurés.
- QR scanné sur téléphone via réseau mobile ; conservation et capacité pour le volume final.
- Endurance, raccourcis Windows et gestes tactiles sur l’écran du photomaton.

État de facturation Supabase déclaré sans souscription payante créée ; affichage détaillé du
forfait non relu via MCP restreint. Aucun upgrade ni appel OpenAI pendant les vérifications.

## Auto-test du paquet

`ColorsParty.exe --self-test <dossier>` exerce les quatre combinaisons avec des dépendances
fictives imposées : capture, montage, raster GDI, journal d’impression, ZIP avec originaux,
QR, PIN et stockage des credentials. Il ignore les réglages utilisateur pour ne jamais
activer un appareil ni une API. Version et OS sont consignés dans `self-test.json`.

`--smoke-ui <dossier>` ouvre la vraie fenêtre de cet exécutable, avec un profil isolé,
enregistre ses pixels et ferme automatiquement. Ce mode ne prouve que le démarrage natif
et son rendu d’accueil ; la recette métier complète est celle des tests headless séparés.
La CI doit exercer ces deux modes **après extraction du ZIP** avant toute release.

## Fiabilité du simulateur HTTP

Le [premier contrôle de PR8](https://github.com/djnivek/PhotoBooth/actions/runs/41106370111)
a révélé une course dans le faux serveur des tests : son `HashSet` de travaux et ses compteurs
étaient modifiés par les deux éditions concurrentes sans synchronisation. Un POST fictif accepté
pouvait ainsi rester vu comme `queued`. Les accès partagés du simulateur sont désormais verrouillés,
et un scénario de 32 séances concurrentes vérifie résultats et comptage exacts.
Ce correctif porte uniquement sur le simulateur de tests ; aucun code du paquet0.2.0 n’a changé.
