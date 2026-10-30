# Validation — 30 octobre 2026

**Application et API codées ; simulation native et partage distant vérifiés. Aucun Canon,
aucune feuille DNP, aucun lancement sur le Windows10 cible, aucune édition OpenAI facturée.**

| Vérification | Résultat |
| --- | --- |
| Solution Release .NET10 | 0 avertissement, 0 erreur sur Mac arm64 |
| Métier | 23 assertions : quatre parcours, séquences, résultats, PIN, impression explicite |
| Imaging | 89 assertions : huit EXIF réels, pixels, ordre, montage, DPI, fichiers et SHA |
| Runtime | 29 assertions : cycle complet, persistance, annulation et erreurs |
| Préparation Windows | 18 assertions : BGRA, axes DPI distincts, zone imprimable, journal avant effet externe |
| Canon WPD | 25 assertions avec transport fictif : séquence visée/capture, quatre vrais fichiers, RAW+JPEG, timeout, aucun redéclenchement |
| Client cloud | 34 assertions : quatre éditions ordonnées, originaux envoyés une fois, ZIP 5/9 fichiers, reprise sans facturation supplémentaire, fallback explicite, credentials |
| UI native rendue headless | Accueil1280/1024, visée fictive, décompte, capture, résultat, export impression, réglages, code erroné puis valide et fermeture protégée |
| API Deno | 9 tests, typecheck et lint : auth, idempotence, réservation, erreurs ambiguës, originaux requis, hash et expiration |
| Galerie préparée | 5 tests DOM sans navigateur : 1/4 liens, archive, jeton hors URL de requête, expiration, rejet d’URL étrangère |
| OpenAPI3.0.3 | Validation formelle réussie avec openapi-spec-validator0.7.2 |
| CI Windows initiale | [Run41088358348](https://github.com/djnivek/PhotoBooth/actions/runs/41088358348) réussi : PR4, WindowsServer2022, tests natifs headless et simulation |
| CI Windows cloud | [Run41096255110](https://github.com/djnivek/PhotoBooth/actions/runs/41096255110) réussi sur le commit de PR5 : client, DPAPI, UI et API simulées |
| Supabase réel | ZIP 5 fichiers puis ZIP 9 fichiers (4 originaux + 4 effets locaux + planche), octets vérifiés, publication rejouée, 0 génération |
| Isolation distante | Quatre tables RLS activées, aucun SELECT anon/authenticated ; bucket privé ; budget started=0 relu |
| Galerie hébergée | Non : API Pages retourne422, forfait incompatible avec dépôt privé ; QR ZIP disponible |

Les captures de pixels réels Avalonia/Skia sont versionnées dans `.impeccable/review/`.
Ce sont des rendus headless, pas des screenshots d’une machine Windows physique. La revue
Impeccable reste limitée aux états observés. L’accès navigateur est désactivé dans Work :
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

- Paquet autonome lancé réellement, puis Windows10Pro22H2 exact (distinct de Server2022).
- Liaison caméra, version/runtime Canon retenu, vraie live view, autofocus/flash et transfert.
- Pilote DS620, paramètres 4×6, feuille réelle, marge et couleur.
- Un appel OpenAI manuel puis quatre : modèle disponible, résultat, temps et coût mesurés.
- QR scanné sur téléphone via réseau mobile ; conservation et capacité pour le volume final.
- Endurance, raccourcis Windows et gestes tactiles sur l’écran du photomaton.

État de facturation Supabase déclaré sans souscription payante créée ; affichage détaillé du
forfait non relu via MCP restreint. Aucun upgrade ni appel OpenAI pendant les vérifications.
