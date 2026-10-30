# Handoff actif : Colors Party by Estelle

30 octobre 2026. Anniversaire le 31. Lire l’état Git/PR et les reçus avant de poursuivre. Les phases codé, testé localement, CI Windows, fusionné, déployé, publié et testé physiquement sont distinctes.

## Mise à jour 0.3.1 en validation

Branche `feat/offline-secure-gallery`. Standard hors ligne avec cloud activé, QR vers une galerie originaux/métamorphoses, limites serveur et arrêt administrateur. Migration de protection appliquée ; test transactionnel annulé réussi, compteur resté à zéro, audit sécurité Supabase sans alerte. Tests navigateur/captures, publication et release encore à confirmer dans le reçu final. Aucun appel payant autorisé dans ces tests.

La galerie utilise le projet Vercel dédié `colors-party-estelle` dans Kevin's projects. Seuls les fichiers statiques de `web/gallery/` sont publics. Les photos restent privées dans Supabase. Le dépôt GitHub reste privé.

## Livraison 0.3.0

[PR12](https://github.com/djnivek/PhotoBooth/pull/12) fusionnée sur `d217c000d0cc6efce73c965d95e311f2d9f4209f`. [Version Windows0.3.0 publiée](https://github.com/djnivek/PhotoBooth/releases/tag/v0.3.0), validation complète Mac et Windows réussie, backend déployé, revue visuelle terminée. [Reçus et captures](evidence/colors-party/README.md). La release contient l’application exacte construite depuis ce commit ; les ajouts de documentation/tests de livraison ne changent pas son code.

- Nouvelle identité explicitement demandée et illustrations approuvées par Kevin.
- Au naturel ou Métamorphose, une ou quatre poses. Six univers et La Surprise du Chef.
- Catalogue public backend, six entrées embarquées, mémoire seulement, une requête initiale et actualisation opérateur. Pas de requête à chaque ouverture. [Ajouter un univers](UNIVERS.md).
- Prompts serveur : identité et corpulence conservées par consigne, costumes et décor variables. Scène choisie figée par séance, y compris après retrait du catalogue.
- Décompte 3,2,1, première visée et préparation attendues à chaque pose, transfert avant reprise.
- Animation légère, phrases par thème, réduction du mouvement, arrêt des timers hors traitement, une seule image vidéo en attente de rendu.
- Code facultatif de quatre caractères, ancien code conservé pour migration. Plein écran garde les réglages ouverts ; maintien de trois secondes en bas à gauche ensuite. Le coin ne recouvre pas Retour ou Annuler.
- PNG générés et prompts exacts versionnés, captures avant/après durables. Critique Claude Opus5.5 Max terminée sur le brief ; pas une revue matérielle ou de code.

## État des liaisons

API Supabase existante avec originaux et résultats privés, ZIP et QR signé, déjà vérifiée avec des mires sans génération. Le nouvel endpoint et sa migration sont déployés. Huit vérifications distantes ont confirmé catalogue public, auth, choix explicite et conflits ; aucun upload ou appel de génération. [Reçu](evidence/colors-party/backend.json). La clé OpenAI est encore absente. État historique 0.3.0 : Pages était refusé pour le dépôt privé et le QR pointait vers le ZIP. Le nouveau parcours 0.3.1 est décrit ci-dessus.

**Canon : point matériel prioritaire.** FC.SDK3.2.1021 via le pilote WPD existant n’atteste pas de l’autofocus sur le 4000D. L’accusé de commande n’est pas une netteté confirmée. L’app ne le transforme pas en succès. Pour demain : objectif MF, netteté réglée à la distance des invités, case « Mise au point manuelle vérifiée sur le Canon », puis Enregistrer le matériel. Vérifier vrai JPEG et retour de visée entre les quatre poses. L’autre voie EDSDK exige un SDK légalement obtenu, absent de ce dépôt. Ne pas copier les DLL de DSLR Booth ni remplacer son pilote.

La DNP et son papier restent à tester réellement : JPEG 1800×1200, 300dpi, mise en page sans recadrage, contrôles de pixels et SHA déjà couverts. Un job spooler accepté n’est pas une preuve de sortie papier.

## Secrets et données

Repo privé `djnivek/PhotoBooth`, Mac `/Users/kevinmachado/PhotoBooth`. Supabase `frkckrvsdsiwivdfyjau`, organisation dédiée, Paris. Le MCP ne voit toujours que l’ancienne organisation ; la CLI authentifiée accède au projet. Aucun autre projet touché, aucun upgrade.

Ajouter `OPENAI_API_KEY` dans [Secrets](https://supabase.com/dashboard/project/frkckrvsdsiwivdfyjau/functions/secrets), jamais dans l’app. Code borne distinct dans `.local/booth-access-key` et `.local/booth-provisioning.json` sur ce Mac, privés. Transfert séparé à Kevin, jamais Git, release ou conversation. DPAPI protège le code sur chaque compte Windows ; le saisir de nouveau sur l’autre PC.

Le dossier `%LOCALAPPDATA%\EstellArt` est conservé intentionnellement pour ne pas perdre les photos/réglages. Pas de purge automatique. « Originaux » signifie JPEG non retouchés normalisés à 2560px, pas RAW capteur. Liens expirés à sept jours sans suppression automatique des objets.

## Tests restants, dans l’ordre

1. PC sans matériel : extraire ColorsParty.exe, tester une/quatre poses, six styles et Chef, aperçu/Imprimer vers fichier, réglages, code et maintien souris/tactile.
2. Mettre la clé serveur, connecter la borne. Kevin déclenche volontairement un essai OpenAI payant avec une photo autorisée ; juger identité, délai, puis quatre photos. Vérifier le QR sur téléphone.
3. Photomaton : fermer DSLR Booth/EOS Utility, tester Canon en MF confirmé, quatre photos nettes, annulation et reconnexion. Puis DS620 4×6, une mire et quatre poses sur une seule feuille.
4. Vérifier réseau, espace, consommables et plusieurs séances successives. Le Windows10 exact et l’ancien i7 restent distincts de la CI Server2022.

## Décisions différées

Stockage Free conservé à la demande de Kevin ; revoir après les essais réussis, avant 600 photos avec originaux/résultats/archives. Aucun test automatisé ne doit appeler OpenAI. Pas de promesse de visages identiques au pixel près. Les raccourcis système Windows restent possibles. Pas de reprise d’ancienne séance dans l’UI malgré les journaux conservés. Ne pas effacer un journal pour forcer une impression incertaine.

## Reproduction et livraison

`sh scripts/verify.sh`, ou `./scripts/verify.ps1` sur Windows. Sur ce Mac : `PHOTOBOOTH_DOTNET=/tmp/photobooth-dotnet/dotnet`, `DOTNET_CLI_HOME=/tmp/photobooth-dotnet-home`. Tests C# exécutables, Deno et Node ; pas dotnet test. Construire avec `--disable-build-servers -m:1`, puis tests `--no-build`.

`./scripts/package-windows.ps1 -Version 0.3.1` publie un ZIP autonome, l’extrait puis exécute `--self-test` et `--smoke-ui` sur Windows. Release privée, SHA256, captures et reçus. Sources seules dans Git. PR autorisée à être ouverte puis fusionnée après vérification ; revenir sur main propre. Aucun auteur ou co-auteur assistant.
