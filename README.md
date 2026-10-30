# Estell’Art · PhotoBooth

Photomaton personnel pour l’anniversaire d’Estelle, le 31 octobre 2026. Dépôt privé.

**Version Windows 0.2.1 disponible pour la répétition.** 1 ou 4 poses, visée, décompte animé de 5 secondes,
original ou style IA surprise, aperçu, une planche 4×6, impression facultative et QR de téléchargement.
Le matériel cible est un Canon EOS 4000D et une DNP DS620 sous Windows 10 Pro 22H2 x64.

## Commencer

- [Télécharger Estell’Art pour Windows](https://github.com/djnivek/PhotoBooth/releases/tag/v0.2.1) : extraire le ZIP puis lancer `EstellArt.exe` ; dépôt privé, connexion GitHub requise.
- [Captures de l’interface](https://github.com/djnivek/PhotoBooth/pull/9) et [preuves Windows](docs/evidence/windows-0.2.1/README.md).
- [Handoff actif](docs/HANDOFF.md) : état exact, reprise sur PC et éléments manquants.
- [Progression et PR](docs/PROGRESS.md) : lots fusionnés et portes restantes.
- [Recette Windows](docs/REPRISE_WINDOWS.md) : répétition ce soir, matériel demain.
- [Validation](docs/VALIDATION.md) : preuves, commandes, limites.

Par défaut, tout est simulé : aucune caméra, aucune impression et aucun appel payant.
Dans Réglages, on peut sélectionner des photos locales pour répéter le vrai parcours.
Le bouton de test d’impression exporte le JPEG exact qui serait envoyé à l’imprimante.

```sh
dotnet run --project src/PhotoBooth.App
```

Sources : C# / .NET 10 / Avalonia 12.1.4, SkiaSharp, adaptateurs Windows Canon WPD/EDSDK et GDI, API Supabase.
Le paquet Windows est autonome : aucune installation .NET nécessaire. Le ZIP extrait a été exécuté
sur Windows Server 2022, avec quatre parcours simulés et une vraie fenêtre vérifiée à deux tailles.
Ces essais ne remplacent pas le lancement sur Windows 10 ni les périphériques du photomaton.

## En ligne

La clé `OPENAI_API_KEY` appartient aux secrets du projet Supabase, jamais à l’application.
La borne utilise un code distinct, protégé avec DPAPI sur Windows. OpenAI est appelé par le serveur
en arrière-plan, avec `gpt-image-2.5-sunburst`, qualité `xhigh`, une édition par pose et deux en parallèle.
L’original est envoyé une fois et fourni au modèle par une URL signée temporaire.
Le ZIP contient les résultats, la planche et les originaux des séances IA.

Le partage réel de quatre images synthétiques a été vérifié sur Supabase, sans génération.
La galerie web est préparée mais GitHub Pages a refusé ce dépôt privé avec le forfait actuel :
le QR ouvre donc directement le ZIP signé. Aucun abonnement supplémentaire n’a été activé.

**Encore nécessaires :** clé OpenAI et essai manuel payant, exécution Windows 10 et vraies
liaisons caméra/imprimante. La connexion Canon utilise le pilote Windows existant sans SDK
propriétaire par défaut ; sa compatibilité EOS4000D doit être vérifiée sur l’appareil. Le plein écran demande un code pour
quitter l’application ; il ne neutralise pas les commandes système Windows.

La [revue finale](docs/reviews/2026-10-30-final-review.md) conserve l’avis ciblé de Claude Opus 5.5,
les quatre corrections et les tests de régression. Le stockage reste au forfait actuel jusqu’aux essais réussis.

## Vérifier et comprendre

```sh
sh scripts/verify.sh        # Mac/Linux : .NET 10, Deno 2, Node 22+
# Windows : ./scripts/verify.ps1
```

Les tests C# sont des exécutables d’assertions, pas `dotnet test`. Les tests réseau remplacent
HTTP par des réponses fictives ; aucune clé n’est requise et aucune génération n’est facturée.

| Document | Contenu |
| --- | --- |
| [Cahier des charges](docs/CAHIER_DES_CHARGES.md) | Parcours demandé |
| [Architecture](docs/ARCHITECTURE.md) | Décisions effectivement implémentées |
| [API](docs/API.md) / [OpenAPI](contracts/openapi.json) | Routes réelles et authentification |
| [Infrastructure](docs/INFRASTRUCTURE.md) | Déploiement et secrets |
| [Coûts](docs/COUTS.md) | Stockage, crédits et limites |
| [Sources](docs/SOURCES.md) | Documentation officielle consultée |
| [Design](DESIGN.md) | Direction Impeccable et composants natifs |

Les photos, secrets, SDK propriétaires, pilotes et produits de build sont exclus de Git.
