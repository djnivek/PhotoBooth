# Handoff actif : Colors Party by Estelle

## Admin Mac accessible via Tailscale

Le 31 octobre, le checkout Mac a été actualisé sur main `e602b40`, puis le service utilisateur `local.kevin.photobooth-admin` a été installé sur `127.0.0.1:8787`. [Rapport privé](https://macmini-de-kevin.tail8f2f33.ts.net/photobooth/api/report?days=7&fixtures=0) : identifiant `admin`, mot de passe dans `artifacts/admin-local/access-token` sur ce Mac. Ce jeton admin distinct change au redémarrage ; ne pas publier sa valeur. [Procédure, arrêt et redémarrage](ADMIN_LOCAL.md).

Correctifs nécessaires au vrai Mac : CLI `db query --linked` sans l'option non reconnue `--project-ref`, vérification du projet lié avant exécution, et prise en charge du tableau JSON renvoyé par la CLI. Connexion HTTP Basic ajoutée au navigateur en conservant Bearer, les restrictions d'hôte/origine et la lecture seule. Cinq tests admin réussis ; vraie lecture du rapport par le service et par HTTPS Tailscale, certificat et refus d'accès vérifiés. Les autres routes privées sont intactes. Aucun changement de l'app Windows, du backend déployé ou du budget OpenAI. [Reçu](evidence/mac-admin/service.json).

L'admin actuel est un rapport JSON. Le tableau de bord visuel et le test depuis un autre appareil du tailnet restent distincts de cette activation.

## Version 0.7 — monitoring et admin local

Kevin demande la collecte métier durable puis le dashboard plus tard sur Mac. Il autorise explicitement la PR et sa fusion. PR17 (version 0.6 déjà approuvée) fusionnée sur b93740a ; travail 0.7 dans `feat/monitoring-local-admin`.

API 0.7 et migration `20261031103145_booth_monitoring.sql` appliquées, sécurité sans alerte. Galerie Vercel `dpl_6jaKcfRSsQ6dgJKHNFShM3FR49eP` READY sur le domaine habituel. Aucun abonnement ni budget de génération modifié. Tests synthétiques uniquement, compteur de générations conservé.

Table privée `booth_metrics` : choix, captures sauvegardées, transferts HTTP mesurés, réponse OpenAI valide et durée fournisseur, ouvertures de galerie, images entrées dans le viewport, clics de téléchargement. Dédoublonnage et file locale durable ; simulations exclues par défaut. Pas de reconstruction des captures historiques ou de personnes uniques. [Définitions](MONITORING.md).

Tests : 83 contrôles cloud, 25 tests Deno, 20 tests galerie et 3 tests admin ; autres suites C# et parcours UI passent. Validation SQL annulée : moyennes, médiane/p95, filtrage fixture, dédoublonnage et droits. Production : 49 contrôles de partage, deux navigateurs restent ouverts pendant la progression, quatre vrais clics/téléchargements synthétiques suivis. Admin local temporaire a lu les vrais rapports et refusé une requête sans jeton ; serveur fermé après test.

Le socle admin est une API JSON en lecture seule, sans dashboard graphique à ce stade. Bind fixe 127.0.0.1, jeton local éphémère, requête Supabase fixe via CLI connectée. [Procédure Mac et Tailscale](ADMIN_LOCAL.md). Aucun serveur permanent ou proxy Tailscale lancé sur Windows. Validation sur le vrai Mac/Tailscale et matériel Canon/DNP à faire plus tard avec Kevin. Il faut le client 0.7 pour les compteurs borne ; le site et les nouvelles générations sont instrumentés côté serveur.

La section 0.6 ci-dessous est historique. La PR, la CI et le paquet 0.7 sont consignés dans les reçus et la description de livraison.

## Mise à jour 0.6 — décision la plus récente de Kevin

Kevin remplace explicitement la précédente décision paysage/xhigh : **Sunburst direct via Images edits, sans Luna, qualité medium, sortie 1024 × 1024**. Puis il demande deux séances pouvant se chevaucher, jusqu'à huit images simultanées, une PR avec aperçus et la mise à jour Supabase. La planche reste 6 × 4 pouces (1800 × 1200, 300 dpi), avec carrés complets à gauche et logo/thème à droite. Au naturel conserve sa composition.

Code 0.6 prêt sur la branche `feat/direct-square-metamorphosis`. Migration `20261031094041_direct_image_concurrency.sql` créée par CLI et dry-run validé : huit travaux non résolus, seize départs/minute, budget total et compteur conservés. Backend : 22 tests et Deno check/lint ; 437 assertions C# (26 + 231 + 54 + 26 + 28 + 72), 18 tests galerie, OpenAPI et parcours UI étendu réussis. Le test client attend les huit départs avant le moindre résultat. Revue indépendante des planches : ship, limitée aux thèmes intégrés et fichiers raster ; DNP absente.

Le ZIP 0.6 extrait a passé les quatre parcours simulés et les fenêtres natives 1280 × 800 et 960 × 700. SHA256 : `9da77f589bca34f7bdf1ecfd7f5ed1f41a13531f15eaab78581a81320398dd55`. [Reçu local](evidence/direct-square/delivery.json).

**Activation 0.6 terminée après confirmation explicite de Kevin.** La planche est approuvée. Huit est un plafond de traitements en cours, pas un lot obligatoire : une séance de quatre poses lance ses quatre éditions. Kevin prévoit au plus une nouvelle séance toutes les deux minutes. Migration appliquée, fonction déployée, métadonnées Sunburst/medium/1024 vérifiées. 48 contrôles de partage réel : arrivée progressive, QR stable, dix assets et neuf entrées exactes du ZIP. Chromium a observé la vraie galerie en ordinateur et mobile passer automatiquement d'un à quatre résultats, puis afficher le ZIP sans changer d'URL. Réservations SQL : huit acceptées, neuvième refusée, cadence, budget et arrêt vérifiés avec annulation complète ; compteur resté à 1. RLS et droits privés confirmés, audit sécurité sans alerte, migrations à jour. Aucun appel OpenAI pendant ces tests.

[PR17](https://github.com/djnivek/PhotoBooth/pull/17) ouverte. [CI Windows 41190537146](https://github.com/djnivek/PhotoBooth/actions/runs/41190537146) réussie sur b2a0e12 : suite complète, Chromium, ZIP extrait/exécuté. Aucun merge ni release 0.6 effectué. Utiliser le paquet 0.6 pour tester la nouvelle planche ; les anciens exécutables conservent leur ancien compositeur. Le déploiement et les plafonds ne modifient pas l'abonnement Supabase. Pro reste recommandé pour le stockage de la soirée, à partir de 25 USD/mois hors OpenAI et surconsommations. Aucun achat fait.

Voir `docs/evidence/direct-square/` pour les planches choisies. Les durées de 16 s citées par Kevin sont ses observations Playground. Aucun plafond de quatre/huit invocations n'est documenté pour Free ; la durée maximale par worker est de 150 s contre 400 s en Pro. Pour la soirée, Pro est recommandé pour stockage/egress si plusieurs centaines de poses sont maintenues. La rétrogradation donne un avoir d'organisation, pas un remboursement carte. Aucun changement de forfait effectué.


## Livraison locale 0.5.0, 31 octobre au matin

Code et paquet Windows préparés dans `artifacts/optimization-0.5.0/` ; pas encore de release GitHub ni de CI de cette version. Base Git `9c518036b5b9b3b6278c13320233885ce75dca03` avec modifications locales. ZIP : `ColorsParty-0.5.0-win-x64.zip`, SHA256 `2becf3afdacf3c05eb015fcff6bceca74c76e49141f3cdce2c94c6c90247e78f`.

Modèles réellement configurés : `gpt-image-2.5-sunburst` pour l’image, `gpt-6-luna` pour Responses. Qualité `xhigh`, JPEG95, sortie **1536×1024 conservée explicitement par Kevin**. Aucun changement de modèle, qualité ou service tier. La présence de la clé a été vérifiée sans la révéler ; Kevin avait déjà réalisé son premier essai payant. Aucun appel OpenAI dans nos vérifications automatiques.

Copies de génération privées : côté long de 1536px maximum, JPEG90, orientation et cadrage conservés, pas d’agrandissement. Originaux et impression gardent leurs octets habituels. Quatre éditions indépendantes peuvent partir en parallèle ; places partagées entre séances, deux uploads et deux téléchargements simultanés. Les originaux s’archivent pendant le calcul. Le résultat fournisseur est stocké directement puis vérifié par SHA sur le PC, sans second encodage ni réenvoi. Le ZIP est assemblé côté Supabase. Plafonds serveur inchangés : quatre travaux non résolus, huit départs/minute, 2400 éditions. Un 429 explicite libère sa place sans relance ; les erreurs ambiguës restent incertaines.

285 contrôles C# (26 métier, 94 imaging, 50 runtime, 26 impression, 28 Canon fictif, 61 cloud), parcours UI étendu, 19 tests Deno avec check/lint, 18 contrats galerie, 10 cas Chromium et OpenAPI validés. Le test de concurrence attend les quatre départs avant de rendre le moindre résultat. Le ZIP extrait a passé ses quatre modes simulés et l’ouverture native Windows 11 à 1280×800 et 960×700. Pas de Canon ni de DNP utilisés.

Supabase **0.5.0 déployé**, migration `20261031075647_optimized_image_transport.sql` appliquée, audit sécurité sans alerte. Galerie Vercel publiée : `dpl_4s5RAwe6WMUgzvZmq6iKD8D9JPLu`, domaine habituel. Partage réel synthétique : 47 contrôles, QR anticipé et stable, arrivée progressive, entrées privées absentes de la galerie, dix assets vérifiés dont les neuf fichiers exacts du ZIP serveur, publication répétable. Chromium a vérifié la production à 1280×900 et 390×844. Compteur payant resté à 1 avant/après ces tests.

Mesure synthétique de quatre images détaillées, sans portrait ni résultat OpenAI : copies d’entrée 8,17→1,28 Mo (−84,3 %), upload total estimé de l’ancien/nouveau circuit 24,22→9,95 Mo (−58,9 %), préparation locale 341ms sur ce PC. Ce n’est ni une mesure 4G ni une latence de génération. Les petites copies restent à juger sur un vrai portrait autorisé.

Proposition de profil rapide, non activée : essayer d’abord Sunburst/high sur un portrait autorisé, puis Flare/high si nécessaire ; comparer fidélité, délai et usage avant adoption. Le mode Fast/priority de Responses ne garantit pas, dans la documentation, une accélération du moteur image utilisé comme outil. Ne pas activer un tier premium sans comparaison et décision de Kevin.

L’interface 0.4 est conservée : Réessayer le partage seulement après échec, Une nouvelle séance bien visible, loader et délai indicatif d’une à deux minutes. Le QR permet de suivre les photos pendant que la séance suivante commence. **L’application doit rester ouverte** pour terminer les séances en arrière-plan. Galerie actualisée toutes les quatre secondes ; aucune génération ne peut partir du téléphone. Revue Impeccable : correction du premier écran mobile résolue.

Reçus : `artifacts/optimization-0.5.0/delivery.json`, `transfer-benchmark.json`, `live-smoke/receipt.json`, `live-smoke/browser.json`, `package-checks/`. Revue visuelle inchangée : `artifacts/progressive-0.4.0/review.json`. La 0.3.1 ci-dessous reste la dernière release GitHub publiée.

30 octobre 2026. Anniversaire le 31. Lire l’état Git/PR et les reçus avant de poursuivre. Les phases codé, testé localement, CI Windows, fusionné, déployé, publié et testé physiquement sont distinctes.

## Dernière release GitHub 0.3.1

[PR14](https://github.com/djnivek/PhotoBooth/pull/14) fusionnée sur `82766b02b86ba4fcbe903ffea0ceaadfe99fe491`. [Version Windows 0.3.1 publiée](https://github.com/djnivek/PhotoBooth/releases/tag/v0.3.1) depuis ce commit exact, après la [CI Windows sur main](https://github.com/djnivek/PhotoBooth/actions/runs/41139279362). Le ZIP publié a été retéléchargé : 81 359 112 octets, SHA256 `a7d2f00aafafbe9043c1fa67e0ab8e46a7f8103680aa97c31b87fa4bacd1c72a`, identique au paquet extrait et exécuté en CI. [Reçu de livraison](evidence/offline-gallery/delivery.json).

Standard hors ligne avec cloud activé, QR vers une galerie originaux/métamorphoses, limites serveur et arrêt administrateur. Migration de protection appliquée ; test transactionnel annulé réussi, compteur resté à zéro, audit sécurité Supabase sans alerte. Suite complète Mac et Windows réussie, Chromium ordinateur/mobile, paquet Windows extrait/exécuté et revue visuelle terminés. Le site est déployé et ses dix fichiers signés ont été vérifiés octet par octet. [Avant/après et reçus](evidence/offline-gallery/README.md). Aucun appel OpenAI payant effectué.

La galerie utilise le projet Vercel dédié `colors-party-estelle` dans Kevin's projects. Seuls les fichiers statiques de `web/gallery/` sont publics. Les photos restent privées dans Supabase. Le dépôt GitHub reste privé.

## Livraison précédente 0.3.0

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

API Supabase 0.5.0 et migration d’entrées optimisées déployées ; les protections précédentes sont conservées. Voir les reçus 0.5 ci-dessus pour l’état actuel. Les [reçus distants 0.3.1](evidence/offline-gallery/README.md) restent historiques : la clé OpenAI était alors absente. Kevin l’a depuis ajoutée et la configuration de génération est disponible. Les vérifications automatiques n’évaluent toujours pas de rendu réel du fournisseur.

**Canon : point matériel prioritaire.** FC.SDK3.2.1021 via le pilote WPD existant n’atteste pas de l’autofocus sur le 4000D. L’accusé de commande n’est pas une netteté confirmée. L’app ne le transforme pas en succès. Pour la recette de cet après-midi : objectif MF, netteté réglée à la distance des invités, case « Mise au point manuelle vérifiée sur le Canon », puis Enregistrer le matériel. Vérifier vrai JPEG et retour de visée entre les quatre poses. L’autre voie EDSDK exige un SDK légalement obtenu, absent de ce dépôt. Ne pas copier les DLL de DSLR Booth ni remplacer son pilote.

La DNP et son papier restent à tester réellement : JPEG 1800×1200, 300dpi, mise en page sans recadrage, contrôles de pixels et SHA déjà couverts. Un job spooler accepté n’est pas une preuve de sortie papier.

## Secrets et données

Repo privé `djnivek/PhotoBooth`, Mac `/Users/kevinmachado/PhotoBooth`. Supabase `frkckrvsdsiwivdfyjau`, organisation dédiée, Paris. Le MCP ne voit toujours que l’ancienne organisation ; la CLI authentifiée accède au projet. Aucun autre projet touché, aucun upgrade.

`OPENAI_API_KEY` est maintenant présente dans [Secrets](https://supabase.com/dashboard/project/frkckrvsdsiwivdfyjau/functions/secrets), jamais dans l’app. Le code borne a été saisi par Kevin sur ce PC ; DPAPI le protège dans `%LOCALAPPDATA%\EstellArt\cloud-credential`. Le saisir de nouveau sur l’autre PC. Les anciens fichiers `.local/booth-access-key` et `.local/booth-provisioning.json` sur le Mac sont privés ; jamais Git, release ou conversation.

Le dossier `%LOCALAPPDATA%\EstellArt` est conservé intentionnellement pour ne pas perdre les photos/réglages. Pas de purge automatique. « Originaux » signifie JPEG non retouchés normalisés à 2560px, pas RAW capteur. Liens expirés à sept jours sans suppression automatique des objets.

## Tests restants, dans l’ordre

1. Fermer l’ancienne app puis lancer l’exécutable 0.5 extrait. Les essais automatisés sans matériel sont terminés sur Windows 11 ; refaire le parcours opérateur et tester manuellement une/quatre poses, six styles et Chef, aperçu/Imprimer vers fichier, réglages et maintien souris/tactile.
2. Essai réel volontaire avec un portrait autorisé, puis quatre si le résultat convient : fidélité, délai de génération, coûts retournés, QR sur téléphone et continuité après Une nouvelle séance. Faire cette mesure sur la 4G prévue. Pour un profil rapide, appliquer seulement le réglage approuvé après comparaison. Les essais hors ligne simulés sont couverts ; refaire la coupure/rétablissement réelle et Réessayer le partage.
3. Photomaton : fermer DSLR Booth/EOS Utility, tester Canon en MF confirmé, quatre photos nettes, annulation et reconnexion. Puis DS620 4×6, une mire et quatre poses sur une seule feuille.
4. Vérifier réseau, espace, consommables et plusieurs séances successives. Le Windows10 exact et l’ancien i7 restent distincts de la CI Server2022.

## Décisions différées

Stockage Free conservé à la demande de Kevin ; revoir après les essais réussis, avant 600 photos avec originaux/résultats/archives. Aucun test automatisé ne doit appeler OpenAI. Pas de promesse de visages identiques au pixel près. Les raccourcis système Windows restent possibles. Pas de reprise d’ancienne séance dans l’UI malgré les journaux conservés. Ne pas effacer un journal pour forcer une impression incertaine.

## Reproduction et livraison

`sh scripts/verify.sh`, ou `./scripts/verify.ps1` sur Windows. Sur ce Mac : `PHOTOBOOTH_DOTNET=/tmp/photobooth-dotnet/dotnet`, `DOTNET_CLI_HOME=/tmp/photobooth-dotnet-home`. Tests C# exécutables, Deno et Node ; pas dotnet test. Construire avec `--disable-build-servers -m:1`, puis tests `--no-build`.

`./scripts/package-windows.ps1 -Version 0.5.0` prépare un ZIP autonome, l’extrait puis exécute `--self-test` et `--smoke-ui` sur Windows. Release GitHub privée à distinguer de ce paquet local, SHA256, captures et reçus. Sources seules dans Git. Aucun auteur ou co-auteur assistant.
