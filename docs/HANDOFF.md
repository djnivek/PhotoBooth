# Handoff actif — Estell’Art

Mise à jour : 30 octobre 2026. Anniversaire demain. Lire [PROGRESS](PROGRESS.md) et
[VALIDATION](VALIDATION.md), puis regarder l’état Git/PR avant de reprendre.

## État concret

- Application native Avalonia, quatre parcours 1/4 × original/IA, répétition sans matériel.
- JPEG final réel 1800×1200 à 300 dpi, huit orientations EXIF, contrôle SHA, grille sans recadrage.
- Adaptateurs Windows EDSDK/GDI codés ; ni Canon ni vraie DS620 testés.
- API Supabase métier déployée : réservations de génération, originaux privés, ZIP et QR.
- Vérification cloud réelle avec mires seulement, zéro génération. Clé OpenAI à ajouter côté serveur.
- Plein écran/code testé dans le rendu natif headless ; commandes système Windows non bloquées.
- GitHub Pages refusé pour ce dépôt privé : QR vers ZIP, galerie web préparée mais non hébergée.
- Paquet Windows et alternative Canon sans SDK : travaux suivants, pas encore livrés dans ce lot.

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

1. Télécharger/extracter le paquet autonome une fois publié et lancer `EstellArt.exe`.
2. Garder caméra/impression simulées. Tester 1 puis 4, original puis effet local, compte à rebours,
   aperçu et fichier d’impression. Vérifier les photos locales sélectionnées et le code plein écran.
3. Provisionner la connexion borne. Contrôle de connexion sans frais. Clé OpenAI uniquement sur Supabase.
4. Kevin lance volontairement une séance IA avec une photo autorisée : rendu, attente, usage/coût,
   QR téléchargé sur téléphone. Puis quatre si le premier résultat convient.
5. Noter le build exact, les erreurs et les limites sans prétendre avoir testé les périphériques.

## Matériel demain

EDSDK officiel absent ; Canon réclame l’accès développeur. Ne pas supposer que la présence de
DSLR Booth fournit une licence de redistribution de ses DLL. Ne pas modifier son installation.
Une voie WPD/PTP ouverte a été identifiée pour évaluation séparée : ne pas la déclarer compatible
EOS4000D avant l’essai réel. Si EDSDK retenu, comparer l’ABI aux en-têtes de la version reçue,
placer les DLL x64 dans `canon/`, fermer les autres logiciels caméra et vérifier les dépendances VC++.

Installer/sélectionner la DS620 dans Windows, papier 4×6 sans coupe 2×6, une copie. Tester d’abord
une mire, puis quatre poses distinctes. Vérifier feuille réelle, marges, sens et couleurs.
Après une soumission incertaine, regarder le spooler : pas de renvoi automatique.

## Limites et décisions à reprendre

- 600 photos avec originaux + IA + ZIP : Free 1 Go probablement insuffisant ; scénario détaillé
  dans COUTS. Pro recommandé à partir de 25 USD/mois, sans activation tant que Kevin ne décide pas.
- Les originaux du photomaton sont non retouchés mais normalisés en JPEG 2560px, pas RAW capteur.
- Expiration 7 jours des liens, pas suppression des objets. Décider du nettoyage après la fête.
- Pas de réouverture d’ancienne séance par UI, malgré les fichiers/journaux conservés.
- Windows 10 Pro 22H2 à tester ; succès CI Windows Server2022 distinct.
- Revue Claude demandée : contrôle automatique a refusé la transmission du brief privé. Question
  explicite d’autorisation déjà envoyée, encore sans réponse. Aucun brief transmis.
- Nom de travail Estell’Art, choisi pour avancer ; pas d’approbation de nom inventée.

PRs autorisées à être ouvertes puis mergées après validation. Commits/PR en anglais, sans co-auteur
assistant. Garder les preuves UI dans Git, les fichiers privés et builds hors Git. Revenir sur main
à jour et propre après chaque livraison. Aucun test automatisé ne doit devenir payant.
