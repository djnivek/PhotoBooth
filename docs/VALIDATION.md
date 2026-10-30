# Validation — 30 octobre 2026

**Application et API codées ; simulation native et partage distant vérifiés. Aucun Canon,
aucune feuille DNP, aucun lancement sur le Windows 10 cible, aucune édition OpenAI facturée.**

| Vérification | Résultat |
| --- | --- |
| Solution Release .NET10 | 0 avertissement, 0 erreur sur Mac arm64 |
| Métier | 23 assertions : quatre parcours, séquences, résultats, PIN, impression explicite |
| Imaging | 89 assertions : huit EXIF réels, pixels, ordre, montage, DPI, fichiers et SHA |
| Runtime | 43 assertions : cycle complet, persistance, annulation, connexion USB tardive sans chevauchement ni fermeture prématurée, erreur COM après annulation |
| Préparation Windows | 26 assertions : BGRA, axes DPI distincts, zone imprimable, journal avant effet externe et suivi d’impression après changement de séance |
| Canon WPD | 25 assertions avec transport fictif : séquence visée/capture, quatre vrais fichiers, RAW+JPEG, timeout, aucun redéclenchement |
| Client cloud | 39 assertions : quatre éditions ordonnées, originaux envoyés une fois, ZIP 5/9 fichiers, reprise sans facturation supplémentaire, fallback explicitement imprimable, credentials et 32 séances HTTP fictives concurrentes |
| UI native rendue headless | Accueil1280/1024/960, deux séances complètes successives, annulation de connexion lente et erreur COM tardive, export impression, fermeture et accès aux fichiers protégés par code erroné/annulé/valide |
| API Deno | 9 tests, typecheck et lint : auth, idempotence, réservation, erreurs ambiguës, originaux requis, hash et expiration |
| Galerie préparée | 5 tests DOM sans navigateur : 1/4 liens, archive, jeton hors URL de requête, expiration, rejet d’URL étrangère |
| OpenAPI3.0.3 | Validation formelle réussie avec openapi-spec-validator0.7.2 |
| CI Windows initiale | [Run41088358348](https://github.com/djnivek/PhotoBooth/actions/runs/41088358348) réussi : PR4, Windows Server 2022, tests natifs headless et simulation |
| CI Windows cloud | [Run41096255110](https://github.com/djnivek/PhotoBooth/actions/runs/41096255110) réussi sur le commit de PR5 : client, DPAPI, UI et API simulées |
| CI Windows Canon | [Run41098262975](https://github.com/djnivek/PhotoBooth/actions/runs/41098262975) réussi sur PR6 : adaptateur fictif et application native |
| EXE Windows extrait | [Run41105339893](https://github.com/djnivek/PhotoBooth/actions/runs/41105339893) réussi sur main `c4ba833` : paquet autonome extrait, quatre parcours, raster, archives2/3/5/9 fichiers, QR, DPAPI |
| Correctifs 0.2.1 sur Windows | [Run41112773487](https://github.com/djnivek/PhotoBooth/actions/runs/41112773487) réussi sur PR9 `496fff1` : régressions PIN/impression/connexion USB, quatre parcours de l’EXE extrait et fenêtre native ; ZIP téléchargé et SHA vérifié |
| Fenêtre Windows réelle | Pixels bureau et rendu natif à 992×700, puis 960×700 après resize : Réglages, Démarrer et planche visibles dans leurs panneaux ; revue Impeccable acceptée à échelle 1 |
| Supabase réel | ZIP 5 fichiers puis ZIP 9 fichiers (4 originaux + 4 effets locaux + planche), octets vérifiés, publication rejouée, 0 génération |
| Isolation distante | Quatre tables RLS activées, aucun SELECT anon/authenticated ; bucket privé ; budget started=0 relu |
| Galerie hébergée | Non : API Pages retourne422, forfait incompatible avec dépôt privé ; QR ZIP disponible |

Les rendus headless Avalonia/Skia sont versionnés dans `.impeccable/review/`.
Les captures du véritable exécutable Windows sont dans [windows-0.2.0](evidence/windows-0.2.0/README.md) :
Windows Server 2022 sur runner GitHub, pas le photomaton Windows 10. La revue Impeccable reste limitée
aux états observés. L’accès navigateur est désactivé dans Work :
aucune validation visuelle mobile de la galerie n’est revendiquée.

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

`EstellArt.exe --self-test <dossier>` exerce les quatre combinaisons avec des dépendances
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
