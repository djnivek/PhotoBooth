# Revue technique avant branchement

Base relue : `main` au commit `0b447de67d4d692965a778726e99369688a30165`, version 0.8.0. La projection de salle et le retour automatique sont inclus dans cette revue. Le serveur admin a été redémarré depuis cette base ; l'accès HTTPS authentifié répond 200, l'accès anonyme aux statistiques répond 401. Le code d'accès est conservé. Vérification depuis le Mac serveur, pas depuis un autre appareil Tailscale.

## Défaut reproduit et corrigé

Après « Une nouvelle séance » ou le départ avec le QR précoce, `finishingControllers` conservait chaque contrôleur terminé. L'ensemble empêchait sa collecte pendant toute la durée de la soirée, même après la fermeture des ressources réseau. La version 0.8.1 retire la référence après la fin du partage et garantit le nettoyage des index même si la fermeture du contrôleur échoue.

Le test exerce la vraie interface Avalonia avec un transport HTTP simulé : démarrer une métamorphose, partir avec le QR, commencer la séance suivante, laisser finir le partage précédent puis vérifier, par une référence faible, que l'ancien contrôleur peut être collecté. Échec avant correction, réussite après. Le résultat précédent ne remplace pas l'écran de l'invité suivant. Les résultats et photos restent sauvegardés sur disque.

Les [reçus](validation.json) distinguent les tests du code, la CI Windows, l'admin démarré et le matériel absent. Les tests ne déclenchent aucun appel OpenAI payant.

## Revue Claude et traitement des constats

[Rapport initial intégral](claude-report.md), [preuve du modèle réellement observé](claude-receipt.json). CLI 2.1.283, `claude-opus-5-5`, effort `max`, revue en lecture seule de l'instantané assaini et de sources publiques. Claude n'a exécuté aucun test ni manipulé de périphérique. Les vérifications ci-dessous ont été faites séparément.

| Constat | Décision et preuve |
| --- | --- |
| B1 : déclenchement possible après un arrêt de visée en erreur | Corrigé. Toute erreur ou expiration invalide la préparation WPD ; le contrôleur refuse également une erreur de fin de visée. EDSDK refuse de déclencher si sa visée reste active. Tests de refus et de blocage jusqu'à expiration : zéro déclenchement. |
| M1 : dimensions papier DNP | Mesure du pilote réel encore requise. La notice décrit le raster avec débord, sans garantir les valeurs de `DC_PAPERSIZE`. Aucun élargissement spéculatif du sélecteur. Deux nouveaux tests couvrent les rasters 1844×1240 à 300 dpi et 3688×2480 à 600 dpi. Le refus de format est récupérable via M2. |
| M2 : refus avant envoi traité comme une impression incertaine | Corrigé. `PrintNotSubmittedException` n'est émis qu'avant toute tentative de `StartDoc`. Le journal `not_submitted` autorise un nouveau clic explicite sur la même planche vérifiée. Dès l'appel de `StartDoc`, les erreurs restent incertaines et aucun deuxième envoi n'est permis. Tests de refus, reprise explicite, identité de la planche et blocage du doublon. |
| M3 : fermeture caméra masquant l'erreur initiale | Corrigé. La référence est libérée, l'erreur initiale est conservée, le diagnostic secondaire est dans le manifeste et la séance suivante peut commencer. Tests avec échec de capture, d'arrêt de visée et de préparation suivis d'une fermeture en erreur. Un pilote natif qui ne rend jamais la main reste une limite matérielle. |
| M4 : décodage pleine résolution sur le thread d'interface | Corrigé. La normalisation des deux adaptateurs Canon et la conversion du raster d'impression passent par `Task.Run`. Les octets sauvegardés et la planche restent couverts par les tests. Aucun benchmark n'est revendiqué sur l'ancien i7. |
| M5 : effet de répétition silencieux avec le vrai Canon sans cloud | Corrigé. Métamorphose devient indisponible avec une explication ; Au naturel reste utilisable hors ligne. Le test UI échoue avant le garde-fou et passe après. [Captures avant/après](../../evidence/technical-readiness/README.md). |
| M6 : générations incertaines occupant les huit places | Protection conservée. La [procédure opérateur](../../SECURITY.md) précise les limites 8/16, les contrôles en lecture seule et la clôture d'un seul traitement après vérification humaine. Aucun traitement ni quota n'a été modifié. |

Le rapport initial n'avait pas repéré la rétention de `finishingControllers`. Le test de collecte ci-dessus la reproduit et prévaut sur son observation statique d'absence de croissance mémoire. Le verdict matériel du rapport reste applicable : Canon, papier DNP, Windows 10 exact et premier rendu payant ne sont pas certifiés par une revue de code.

## Vérifications sur le matériel

1. Extraire entièrement le dernier ZIP et ouvrir `ColorsParty.exe` sur le Windows 10 exact. Le paquet contient le runtime ; aucun SDK .NET n'est nécessaire. Windows 10 Pro 22H2 ne figure pas dans la [matrice de support .NET 10 actuelle](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md). Une CI Windows Server 2022 réussie ne garantit pas ce lancement. Cela ne prouve pas non plus une incompatibilité d'exécution.
2. Fermer DSLR Booth et EOS Utility, brancher le Canon seul, sélectionner JPEG et vérifier l'alimentation. Régler l'objectif sur MF, faire la netteté à la distance réelle des invités puis confirmer la mise au point manuelle dans les réglages. Tester la visée, une photo puis quatre, avec retour de visée et transfert de quatre JPEG distincts. La [notice Canon EOS Utility](https://gdlp01.c-wss.com/gds/7/0300029847/02/eu-3-9-00-w-im-en.pdf), page 2, inclut le 4000D pour la prise de vue à distance ; elle ne certifie pas FC.SDK/WPD. Ne pas copier les DLL de DSLR Booth ni remplacer son pilote.
3. Sélectionner la file DS620 et le format (6x4) ou PR (4x6), une copie, découpe 2 pouces désactivée. La [notice DNP du pilote Windows](https://dnpphoto.eu/wp-content/uploads/2025/08/DP-DS620_PrinterDriverInstruction_For7810_V1.0.5.2_1_English.pdf), sections 2.2 et 2.5, distingue le format nominal du raster avec débord : 1844×1240 à 300 dpi pour PR (4x6) paysage. Relever d'abord les formats avec la [commande du guide Windows](../../REPRISE_WINDOWS.md). Notre planche 1800×1200 est ajustée au contexte imprimable du pilote. Imprimer une mire puis quatre poses, vérifier une seule feuille, l'ordre, la netteté, les couleurs et les marges. Un numéro de spooler ne signifie pas qu'une feuille est sortie.
4. Couper réellement Internet, faire une puis quatre photos Au naturel et imprimer. Rétablir le réseau et tester le partage, puis une métamorphose volontaire avec un portrait autorisé. Ce dernier essai est payant ; il reste manuel. Scanner le QR sur un téléphone et vérifier originaux, résultats et téléchargements.
5. Faire plusieurs séances consécutives sur l'ancien i7, observer la réactivité, la mémoire, le délai des transferts et la stabilité USB. Les limites de taille, les files de transferts bornées et les tests simulés ne constituent pas une mesure sur cet ordinateur.

En cas de résultat d'impression incertain, vérifier la file et le papier avant toute reprise. Le pilote DNP peut reprendre un travail conservé en mémoire après une panne de consommable ; l'app ne doit pas en envoyer une seconde copie automatiquement. Garder DSLR Booth utilisable jusqu'à la fin de la recette.

## Décisions conservées

L'autofocus WPD reste bloqué tant que le sens du résultat AF n'est pas vérifié sur le 4000D. Le mode MF contrôlé est la voie de recette disponible. L'adaptateur EDSDK optionnel exige un SDK officiellement obtenu et une validation distincte.

Le stockage, les quotas, les secrets et les réglages de génération ne sont pas modifiés par cette revue. Les benchmarks du PC cible et la conformité physique Canon/DS620 restent à compléter lors du branchement. Ne pas présenter une simulation comme une validation matérielle.
