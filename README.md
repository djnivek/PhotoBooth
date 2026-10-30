# PhotoBooth

> **Périmètre actualisé : anniversaire d’Estelle demain.** Voir [la progression](docs/PROGRESS.md)
> et [le handoff actif](docs/HANDOFF.md). Le chiffrage initial décrit une version durable ;
> la livraison en cours vise une borne personnelle avec simulation complète puis validation matérielle.

Photomaton Windows pour **Canon EOS 4000D**, **DNP DS620** et papier **4 × 6 pouces**.
Une ou quatre photos, originales ou transformées par IA dans un style surprise,
une seule planche imprimable et un QR code pour télécharger les photos.

## État au 30 octobre 2026

Préparation technique, pas encore une application utilisable en événement.

- Besoin, architecture, coût, risques matériels et recette Windows documentés.
- Moteur de séance C# exécutable sur Mac : visée demandée pendant le positionnement
  et le décompte, 1/4 prises, modes original/IA, ordre des résultats et impression explicite.
- Gabarit logique 4 × 6, garde contre les doubles impressions, vérification de code
  administrateur avec ralentissement des essais. Ces règles ne constituent pas encore
  une interface Windows, un pilote ou un verrouillage du système.
- Simulateur en ligne de commande et tests sans appareil, sans photo réelle et sans appel facturé.
- Organisation et projet Supabase dédiés créés ; voir [l’état d’infrastructure](docs/INFRASTRUCTURE.md).
- Aucun SDK Canon, secret, original de visiteur ou pilote redistribué.

**Commencer par le [cahier des charges](docs/CAHIER_DES_CHARGES.md).**

| Document | Utilité |
| --- | --- |
| [Architecture et décisions](docs/ARCHITECTURE.md) | Windows natif, Canon, impression, backend et arbitrages |
| [Plan de réalisation et charge](docs/PLAN_REALISATION.md) | Lots, complexité, prérequis et décisions différées |
| [Reprise sur Windows](docs/REPRISE_WINDOWS.md) | Installation, diagnostic matériel, recette terrain |
| [API](docs/API.md) et [OpenAPI](contracts/openapi.json) | Contrat cible, authentification et états |
| [Coûts](docs/COUTS.md) | Free Supabase, consommation image et capacité événementielle |
| [Validation](docs/VALIDATION.md) | Ce qui a effectivement été vérifié et ce qui reste ouvert |
| [Sources officielles](docs/SOURCES.md) | Documentation consultée, datée et limites de preuve |

## Lancer la préparation

Installer un SDK **.NET 10 stable** sur la machine de développement.
Le cœur portable ne dépend d'aucun package NuGet tiers.

```sh
dotnet run --project tests/PhotoBooth.Core.Tests
dotnet run --project src/PhotoBooth.Simulator -- 4 original
dotnet run --project src/PhotoBooth.Simulator -- 4 surprise
```

Les deux premiers arguments acceptent `1|4` et `original|surprise`.
Le simulateur utilise un temps virtuel et produit un gabarit SVG ainsi qu'un manifeste
dans `artifacts/simulation/<session>/`. Il ne prend aucune photo, ne génère aucune image
IA, ne publie aucun lien et n’imprime rien. Les références photo dans son manifeste sont fictives.

La commande de tests est un exécutable d’assertions autonome, **pas `dotnet test`**.
Les contrats matériels sont dans `src/PhotoBooth.Core/HardwareContracts.cs`.

Pour la vérification complète du socle, installer également Deno 2 stable et lancer
`sh scripts/verify.sh` sur Mac/Linux ou `./scripts/verify.ps1` sur Windows.
Le contrôle HTTP déployé et la validation formelle OpenAPI sont décrits dans le reçu de validation.

## Cible Windows

Cible matérielle demandée : **Windows 10 Pro 22H2**, architecture du PC à relever.
Cible applicative proposée : **C# / WPF**, avec un cœur indépendant de Windows.
La version du runtime du futur hôte Windows reste conditionnée au premier essai matériel :
la matrice actuelle .NET 10 ne liste plus Windows 10 Pro 22H2. Une compilation Mac ne prouve
donc ni sa compatibilité sur ce PC ni un support officiel. Voir les décisions [D01/D02](docs/ARCHITECTURE.md).

Le nom retenu est **PhotoBooth**, aligné sur le dossier d’origine ; « PhotoVoth » dans
le premier message est interprété comme une transcription vocale.
