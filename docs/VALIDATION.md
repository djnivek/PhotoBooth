# Validation de la préparation — 30 octobre 2026

## Résultat

**Socle préparé et testé sur Mac. Application Windows, capture réelle, transformation réelle,
galerie mobile, verrouillage système et impression physique non validés.**

| Vérification | Résultat et périmètre |
| --- | --- |
| Compilation Release de `PhotoBooth.slnx` | Réussie, 0 avertissement, 0 erreur ; cœur, simulateur, tests |
| Tests C# | **23/23** ; scénarios et invariants métier, sans I/O matériel |
| Typecheck et lint Deno | Réussis sur la fonction et ses tests |
| Tests fonction API | **3/3** ; santé explicite, routes inactives, méthodes interdites |
| OpenAPI 3.0.3 | Validé avec `openapi-spec-validator` 0.7.2 ; contrat cible, pas implémentation métier |
| Simulateur `4 surprise` | Exécuté ; quatre décomptes, quatre références distinctes, état Ready, aucune impression |
| Projet Supabase | Création Nano demandée, Paris, `ACTIVE_HEALTHY` relu via CLI |
| Endpoint distant `GET /health` | **HTTP 200**, JSON de préparation et `Cache-Control: no-store` vérifiés |
| Endpoint distant `POST /health` | **HTTP 405**, mutation refusée |
| Endpoint distant `POST /sessions` | **HTTP 404**, non implémenté, aucune génération possible |
| Endpoint distant `GET /downloads/resolve` | **HTTP 404**, non implémenté |
| Secrets | `.local/supabase-db-password` mode 0600 et ignoré par Git ; aucun secret dans les sources |
| GitHub | Dépôt privé confirmé via `gh repo view` ; état final du commit vérifié après publication |

## Commandes reproductibles

Sur Mac/Linux avec .NET 10 et Deno 2 :

```sh
sh scripts/verify.sh
dotnet run --project src/PhotoBooth.Simulator -- 4 surprise
```

Lors de cette préparation, le SDK .NET 10.0.403 (runtime 10.0.14) a été installé dans
`/tmp/photobooth-dotnet`. Deno était en version 2.9.2. Commande exacte de vérification :

```sh
DOTNET_CLI_HOME=/tmp/photobooth-dotnet-home \
DOTNET_CLI_TELEMETRY_OPTOUT=1 \
PHOTOBOOTH_DOTNET=/tmp/photobooth-dotnet/dotnet sh scripts/verify.sh
```

Un premier build de solution est resté silencieux dans l’environnement isolé. Il a été
arrêté, puis le script a été adapté pour exécuter MSBuild en un seul processus et sans
serveurs de build. Cette configuration a terminé la compilation Release en 1,79 s.
La cause interne du blocage initial n’a pas été démontrée ; ce n’est pas un défaut Windows observé.

Validation formelle du contrat dans un environnement Python temporaire :

```python
import json
from openapi_spec_validator import validate
with open("contracts/openapi.json") as source:
    validate(json.load(source))
```

Contrôle public sans clé API :

```sh
curl --fail-with-body https://frkckrvsdsiwivdfyjau.supabase.co/functions/v1/photobooth-api/health
```

Réponse vérifiée :

```json
{"service":"photobooth","api_version":"0.1.0","stage":"preparation","production_ready":false,"session_api_enabled":false,"image_generation_enabled":false}
```

## Couverture métier

Les quatre combinaisons de prises/mode, visée demandée pendant le décompte, interdiction
de capture anticipée, absence de doublon de capture, comptage/ordre/propriété des résultats,
style stable, absence de substitution silencieuse par un original, impression explicite,
double appui, soumission incertaine, erreur conservant les originaux, annulation, géométrie
des cadres et code de sortie avec temporisation.

Ces tests s’exécutent sur une machine à états **en mémoire**. Ils ne démontrent pas la
persistance après crash, l’idempotence d’un fournisseur externe ou l’absence de doublons
dans le spooler Windows. Les fichiers photo du simulateur sont des références fictives ;
seuls son gabarit SVG et son manifeste sont générés.

## Gates encore ouverts

1. Runtime/hôte WPF sur Windows 10 Pro 22H2 et statut de maintenance du système.
2. EDSDK sous licence, vraie live view EOS 4000D, flash, autofocus et reconnexion.
3. Mire et tirage papier DS620 4×6, dimensions du pilote et rendu couleur.
4. Persistance SQLite, provisionnement de borne, migrations, RLS, Storage et file de tâches.
5. Modèle image en `xhigh` : clé, solde, fidélité, temps p95 et coût réellement mesurés.
6. Galerie HTTPS, expiration, purge et téléchargement par téléphone via réseau mobile.
7. Interface plein écran, code administrateur persistant et restrictions Windows.
8. Scripts PowerShell sur Windows, installation réelle et endurance événementielle.

Ni CI Windows, ni installation sur un appareil physique, ni impression, ni édition OpenAI
n’ont été exécutées. Le détail de facturation Supabase n’a pas pu être relu via le connecteur
limité aux anciennes organisations ; aucun upgrade payant n’a été demandé.
