# Handoff actif — Estell’Art

Mise à jour : 30 octobre 2026. Anniversaire demain. Lire [PROGRESS](PROGRESS.md) et
[VALIDATION](VALIDATION.md), puis regarder l’état Git/PR avant de reprendre.

## État concret

- Ajustement demandé après la revue : afficher « Surprise » et retirer les mentions de simulation
  de l’interface. Modification livrée dans la PR11 pour la version0.2.2 ; le comportement des appareils, de l’API et de
  l’export d’impression reste inchangé. Les captures incluent désormais le parcours Surprise.
- Application native Avalonia, quatre parcours 1/4 × original/IA, répétition sans matériel.
- JPEG final réel 1800×1200 à 300 dpi, huit orientations EXIF, contrôle SHA, grille sans recadrage.
- Adaptateurs Windows WPD/EDSDK/GDI codés ; ni Canon ni vraie DS620 testés.
- API Supabase métier déployée : réservations de génération, originaux privés, ZIP et QR.
- Vérification cloud réelle avec mires seulement, zéro génération. Clé OpenAI à ajouter côté serveur.
- Plein écran/code testé dans le rendu natif headless ; commandes système Windows non bloquées.
- GitHub Pages refusé pour ce dépôt privé : QR vers ZIP, galerie web préparée mais non hébergée.
- Canon sans SDK propriétaire intégré via FC.SDK3.2.1021/WPD, 25 tests de séquence fictive passés.
- [Paquets Windows](https://github.com/djnivek/PhotoBooth/releases) : chaque version publiée fournit son ZIP, son SHA et ses reçus d’exécution. Télécharger la version la plus récente.
- Accueil Windows vérifié à 992×700 puis 960×700 après redimensionnement ; débordements corrigés. Captures versionnées et ajoutées aux PR 3/5/6/7/8/9.
- Correctifs 0.2.1 fusionnés dans la PR9 : accès aux fichiers protégé par le code, journal d’impression
  lié à la bonne séance, connexion annulée sans chevauchement et crash de la deuxième séance corrigé.
  Quatre régressions reproduites puis corrigées ; contrôles Windows de PR9 et de main réussis.
  Revue ciblée Claude terminée : aucun blocage dans le diff, compléments de tests intégrés.

## Secrets et machines

Sources Mac : `/Users/kevinmachado/PhotoBooth`. Dépôt privé : `djnivek/PhotoBooth`.
Projet Supabase : `frkckrvsdsiwivdfyjau`, Paris, organisation dédiée. CLI autorisé ; MCP ancien
scope. Aucun autre projet touché. Aucun upgrade payant effectué.

Ajouter `OPENAI_API_KEY` dans [Secrets du projet](https://supabase.com/dashboard/project/frkckrvsdsiwivdfyjau/functions/secrets).
Ne pas mettre cette clé dans la borne. Le code borne distinct est dans `.local/booth-access-key`
et `.local/booth-provisioning.json` sur ce Mac, mode0600. Transfert séparé à l’opérateur ; jamais
Git, release ou conversation. La borne le protège avec DPAPI pour l’utilisateur Windows courant.
Si renouvelé sur le PC, mettre à jour aussi `BOOTH_ACCESS_KEY_HASH` côté serveur ; cela change
le propriétaire des nouvelles séances et ne donne pas accès aux anciennes avec le nouveau code.

Données Windows : `%LOCALAPPDATA%\EstellArt` ; sous-dossiers séances, manifestes, originaux,
résultats, planche, journal d’impression et de génération. Pas de purge automatique. Ne pas copier
un credential chiffré DPAPI d’un PC vers l’autre : saisir le code sur chaque machine.

## Commandes de vérification

```sh
sh scripts/verify.sh
# Windows : ./scripts/verify.ps1
# Outil réseau volontaire, mires synthétiques seulement, aucun appel OpenAI :
dotnet run --project tools/PhotoBooth.CloudSmoke -- --share-synthetic-originals
dotnet run --project tools/PhotoBooth.CloudSmoke -- --share-synthetic-edits
```

Sur ce Mac temporairement : SDK `/tmp/photobooth-dotnet/dotnet`,
`DOTNET_CLI_HOME=/tmp/photobooth-dotnet-home`, `AVALONIA_TELEMETRY_OPTOUT=1`.
Le script shell accepte `PHOTOBOOTH_DOTNET`. Deno 2.9.2, Node 22. Tests HTTP entièrement simulés.

## Essai ce soir

1. Télécharger/extracter [le dernier paquet Windows](https://github.com/djnivek/PhotoBooth/releases) et lancer `EstellArt.exe`.
2. Garder caméra/impression simulées. Tester 1 puis 4, original puis effet local, compte à rebours,
   aperçu et fichier d’impression. Vérifier les photos locales sélectionnées et le code plein écran.
3. Provisionner la connexion borne. Contrôle de connexion sans frais. Clé OpenAI uniquement sur Supabase.
4. Kevin lance volontairement une séance IA avec une photo autorisée : rendu, attente, usage/coût,
   QR téléchargé sur téléphone. Puis quatre si le premier résultat convient.
5. Noter le build exact, les erreurs et les limites sans prétendre avoir testé les périphériques.

## Matériel demain

La connexion Canon par défaut utilise WPD/PTP via FC.SDK3.2.1021 et le pilote Windows déjà
présent, sans DLL propriétaire ni remplacement de pilote. Cocher « Utiliser le Canon USB »
et laisser l’option SDK officiel décochée. EOS4000D non testé par nous et non listé dans les
preuves matérielles du mainteneur : premier essai demain, aucune garantie inventée.

Si WPD échoue, l’adaptateur EDSDK officiel reste disponible, mais le SDK est absent.
Ne pas modifier DSLR Booth, ses fichiers ou son pilote. Avec un SDK acquis légalement,
comparer l’ABI à ses en-têtes, placer les DLL x64 dans `canon/` et vérifier les runtimes VC++.
Version DSLR Booth à relever si l’on doit étudier son API ou importer ses photos.

Installer/sélectionner la DS620 dans Windows, papier 4×6 sans coupe 2×6, une copie. Tester d’abord
une mire, puis quatre poses distinctes. Vérifier feuille réelle, marges, sens et couleurs.
Après une soumission incertaine, regarder le spooler : pas de renvoi automatique.

## Limites et décisions à reprendre

- 600 photos avec originaux + IA + ZIP : Free 1 Go probablement insuffisant ; scénario détaillé
  dans COUTS. Kevin demande de conserver le forfait actuel et de réévaluer demain après les essais réussis.
- Les originaux du photomaton sont non retouchés mais normalisés en JPEG 2560px, pas RAW capteur.
- Expiration 7 jours des liens, pas suppression des objets. Décider du nettoyage après la fête.
- Pas de réouverture d’ancienne séance par UI, malgré les fichiers/journaux conservés.
- Windows 10 Pro 22H2 à tester ; succès CI Windows Server 2022 distinct.
- Revue ciblée Claude Opus 5.5, effort max, terminée ; [rapport et traitement des remarques](reviews/2026-10-30-final-review.md).
  Aucun blocage dans le diff. Les deux audits plus larges ont épuisé la sortie sans rapport ;
  ne pas revendiquer une revue intégrale ni une validation matérielle.
- Nom de travail Estell’Art, choisi pour avancer ; pas d’approbation de nom inventée.

PRs autorisées à être ouvertes puis mergées après validation. Commits/PR en anglais, sans co-auteur
assistant. Garder les preuves UI dans Git, les fichiers privés et builds hors Git. Revenir sur main
à jour et propre après chaque livraison. Aucun test automatisé ne doit devenir payant.

## Packaging reproductible

Sur Windows avec PowerShell7 et SDK.NET10 : `./scripts/package-windows.ps1`.
Produit attendu : `artifacts/package/EstellArt-<version>-win-x64.zip` et SHA-256, autonome.
Le script publie en single-file avec extraction des bibliothèques natives, compresse, réextrait
le ZIP, lance cet EXE en `--self-test` puis `--smoke-ui`. Les reçus et une capture de la fenêtre
native vont dans `artifacts/package/diagnostics/`. Aucun secret, appel réseau ou périphérique.
Le CI conserve le paquet et ces preuves ; ne publier la release qu’après leur réussite.

Les fichiers de démarrage sont dans `docs/DEMARRAGE_WINDOWS.txt` (copié LISEZ-MOI.txt au paquet).
Le paquet est personnel et non signé ; la provenance et le SHA doivent correspondre à GitHub.
Reçu historique versionné : [v0.2.1](https://github.com/djnivek/PhotoBooth/releases/tag/v0.2.1), commit
`2e026337ab8341bc37608d55dcc3ea67be0db314`, [run41116597603](https://github.com/djnivek/PhotoBooth/actions/runs/41116597603).
ZIP : 71,148,214 octets ; SHA-256 `e51329e3b769600bb6ae222021968048bbdff85e278520d5a8bcbf74df5f9120`.
Reçus et captures : [evidence/windows-0.2.1](evidence/windows-0.2.1/README.md).
Archive retéléchargée depuis la release et SHA vérifié. Au dernier contrôle de l’API avant cette
revue, la génération restait désactivée faute de clé OpenAI ; relire après son ajout par Kevin.

Pour les versions suivantes, la page Releases contient `package.json`, `self-test.json` et le SHA
correspondant à l’archive exacte. La PR de la modification conserve les captures de l’interface.

Kevin demande des screenshots dans les PR pour commenter le design. Joindre des images versionnées
et des liens de commit immuables à chaque changement d’interface ; distinguer simulation, fenêtre
Windows réelle et matériel. Ne pas retirer les captures après merge.
