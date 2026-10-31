# Colors Party by Estelle · PhotoBooth

Photomaton personnel pour l’anniversaire d’Estelle, le 31 octobre 2026.

**Paquet Windows disponible.** 1 ou 4 poses, visée, décompte animé de 3 secondes,
Au naturel ou Métamorphose, aperçu, une planche 4×6, impression facultative et QR de téléchargement.
Le matériel cible est un Canon EOS 4000D et une DNP DS620 sous Windows 10 Pro 22H2 x64.

## Commencer

- [Télécharger Colors Party by Estelle pour Windows](https://github.com/djnivek/PhotoBooth/releases/latest) : extraire le ZIP puis lancer `ColorsParty.exe` ; une connexion GitHub reste nécessaire tant que le dépôt est privé.
- [Captures de l’interface](docs/evidence/colors-party/README.md), [galerie avant/après et preuves0.3.1](docs/evidence/offline-gallery/README.md).
- [Handoff actif](docs/HANDOFF.md) : état exact, reprise sur PC et éléments manquants.
- [Progression et PR](docs/PROGRESS.md) : lots fusionnés et portes restantes.
- [Recette Windows](docs/REPRISE_WINDOWS.md) : installation et essais sur le PC de la salle.
- [Validation](docs/VALIDATION.md) : preuves, commandes, limites.

Par défaut, le Canon USB et l’impression réelle sont activés. Une seule file DNP DS620 installée est présélectionnée ; aucune imprimante PDF n’est choisie automatiquement. La mise à jour 0.10.1 applique ces réglages une fois aux anciens profils de démonstration, sans changer le code ni la connexion en ligne.
La roue crantée en bas à gauche de l’accueil ouvre les réglages d’un clic, même en plein écran. Le code enregistré est demandé ; sans code, l’accès est direct.
Pour répéter sans matériel, désactiver explicitement le Canon et l’envoi à l’imprimante dans les réglages, puis enregistrer. On peut alors choisir des photos locales ; « Imprimer » exporte `print-preview.jpg`. Ce choix de répétition reste conservé aux lancements suivants.
Métamorphose propose dix sept univers illustrés et La Surprise du Chef. Les huit tables de la soirée sont dans **by Estelle** ; **Les incontournables** contient Colors Party, Halloween et Sous cloche ; les six autres restent dans **Autres**. Pour quatre photos, sélectionner de un à quatre univers répartis de façon équilibrée. Les réglages actualisent le catalogue depuis le serveur sans reconstruire l’application. Voir [les thèmes et leurs illustrations](docs/THEMES_ESTELLE.md).
Voir [la gestion des univers](docs/UNIVERS.md).

```sh
dotnet run --project src/PhotoBooth.App
```

Sources : C# / .NET 10 / Avalonia 12.1.4, SkiaSharp, adaptateurs Windows Canon WPD/EDSDK et GDI, API Supabase.
Le paquet Windows est autonome : aucune installation .NET nécessaire. Le ZIP extrait a été exécuté
sur Windows Server 2022, avec quatre parcours simulés et une vraie fenêtre vérifiée à deux tailles.
Ces essais ne remplacent pas le lancement sur Windows 10 ni les périphériques du photomaton.

## En ligne

La clé `OPENAI_API_KEY` appartient aux secrets du projet Supabase, jamais à l’application.
La borne utilise un code distinct, protégé avec DPAPI sur Windows. Le serveur appelle directement
Images Edits avec `gpt-image-2.5-sunburst`, qualité `medium`, sortie `1024x1024` et une édition par pose.
Jusqu’à huit générations sont partagées entre les séances, avec deux uploads et deux downloads simultanés.
Une copie privée limitée à 1536 pixels, JPEG90 sans recadrage ni agrandissement, est fournie au modèle
par une URL signée temporaire ; les originaux sont archivés séparément.
Le ZIP contient les résultats, la planche et les originaux des séances IA.

Le QR ouvre une galerie hébergée sur Vercel, avec les originaux et métamorphoses réunis par pose, un téléchargement individuel et le ZIP complet. Les photos restent privées dans Supabase. Le partage réel et les dix fichiers signés ont été vérifiés avec des mires, sans génération. Aucun abonnement supplémentaire n’a été activé.

Sans réseau, Au naturel prend les photos, prépare la planche et imprime localement, même si le cloud est activé. Seul le partage attend la connexion et peut être relancé sur le résultat. Métamorphose en ligne nécessite Internet.

L’API exige le secret de la borne pour créer une image. Les jetons QR ne permettent que la lecture. Plafond de soirée, cadence, travaux simultanés et arrêt administrateur sont imposés en base, avant tout appel fournisseur. Voir [la sécurité et les procédures opérateur](docs/SECURITY.md).

**À valider dans la salle :** portrait Métamorphose, QR, verrouillage Windows 10 et vraies
liaisons caméra/imprimante. La connexion Canon utilise le pilote Windows existant sans SDK
propriétaire par défaut ; sa compatibilité EOS4000D doit être vérifiée sur l’appareil. Le plein écran utilise un code facultatif de quatre caractères, demandé après un clic sur la roue crantée de l’accueil. Les sorties courantes au clavier sont bloquées en plein écran ; la protection de Windows lui-même exige un compte kiosque dédié, décrit dans [KIOSQUE_WINDOWS.md](docs/KIOSQUE_WINDOWS.md).

Le pilote WPD ne confirme pas encore l’autofocus du 4000D : le premier essai nécessite une mise au point manuelle vérifiée dans les réglages ou la voie du SDK officiel.

La [critique des styles](docs/reviews/colors-party/claude-styles.md) conserve l’avis de Claude Opus 5.5 Max et les décisions retenues. Les [revues précédentes](docs/reviews/2026-10-30-final-review.md) restent disponibles. Le stockage reste au forfait actuel jusqu’aux essais réussis.

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
