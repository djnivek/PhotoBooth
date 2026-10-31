# Revue technique avant branchement

Base relue : `main` au commit `0b447de67d4d692965a778726e99369688a30165`, version 0.8.0. La projection de salle et le retour automatique sont inclus dans cette revue. Le serveur admin a été redémarré depuis cette base ; l'accès HTTPS authentifié répond 200, l'accès anonyme aux statistiques répond 401. Le code d'accès est conservé. Vérification depuis le Mac serveur, pas depuis un autre appareil Tailscale.

## Défaut reproduit et corrigé

Après « Une nouvelle séance » ou le départ avec le QR précoce, `finishingControllers` conservait chaque contrôleur terminé. L'ensemble empêchait sa collecte pendant toute la durée de la soirée, même après la fermeture des ressources réseau. La version 0.8.1 retire la référence après la fin du partage et garantit le nettoyage des index même si la fermeture du contrôleur échoue.

Le test exerce la vraie interface Avalonia avec un transport HTTP simulé : démarrer une métamorphose, partir avec le QR, commencer la séance suivante, laisser finir le partage précédent puis vérifier, par une référence faible, que l'ancien contrôleur peut être collecté. Échec avant correction, réussite après. Le résultat précédent ne remplace pas l'écran de l'invité suivant. Les résultats et photos restent sauvegardés sur disque.

Les [reçus](validation.json) distinguent les tests du code, la CI Windows, l'admin démarré et le matériel absent. Les tests ne déclenchent aucun appel OpenAI payant.

## Vérifications sur le matériel

1. Extraire entièrement le dernier ZIP et ouvrir `ColorsParty.exe` sur le Windows 10 exact. Le paquet contient le runtime ; aucun SDK .NET n'est nécessaire. Windows 10 Pro 22H2 ne figure pas dans la [matrice de support .NET 10 actuelle](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md). Une CI Windows Server 2022 réussie ne garantit pas ce lancement. Cela ne prouve pas non plus une incompatibilité d'exécution.
2. Fermer DSLR Booth et EOS Utility, brancher le Canon seul, sélectionner JPEG et vérifier l'alimentation. Régler l'objectif sur MF, faire la netteté à la distance réelle des invités puis confirmer la mise au point manuelle dans les réglages. Tester la visée, une photo puis quatre, avec retour de visée et transfert de quatre JPEG distincts. La [notice Canon EOS Utility](https://gdlp01.c-wss.com/gds/7/0300029847/02/eu-3-9-00-w-im-en.pdf), page 2, inclut le 4000D pour la prise de vue à distance ; elle ne certifie pas FC.SDK/WPD. Ne pas copier les DLL de DSLR Booth ni remplacer son pilote.
3. Sélectionner la file DS620 et le papier PR (4x6), une copie, découpe 2 pouces désactivée. La [notice DNP du pilote Windows](https://dnpphoto.eu/wp-content/uploads/2025/08/DP-DS620_PrinterDriverInstruction_For7810_V1.0.5.2_1_English.pdf), sections 2.2 et 2.5, distingue le format nominal du raster avec débord : 1844×1240 à 300 dpi pour PR (4x6) paysage. Notre planche 1800×1200 est ajustée au contexte imprimable du pilote. Imprimer une mire puis quatre poses, vérifier une seule feuille, l'ordre, la netteté, les couleurs et les marges. Un numéro de spooler ne signifie pas qu'une feuille est sortie.
4. Couper réellement Internet, faire une puis quatre photos Au naturel et imprimer. Rétablir le réseau et tester le partage, puis une métamorphose volontaire avec un portrait autorisé. Ce dernier essai est payant ; il reste manuel. Scanner le QR sur un téléphone et vérifier originaux, résultats et téléchargements.
5. Faire plusieurs séances consécutives sur l'ancien i7, observer la réactivité, la mémoire, le délai des transferts et la stabilité USB. Les limites de taille, les files de transferts bornées et les tests simulés ne constituent pas une mesure sur cet ordinateur.

En cas de résultat d'impression incertain, vérifier la file et le papier avant toute reprise. Le pilote DNP peut reprendre un travail conservé en mémoire après une panne de consommable ; l'app ne doit pas en envoyer une seconde copie automatiquement. Garder DSLR Booth utilisable jusqu'à la fin de la recette.

## Décisions conservées

L'autofocus WPD reste bloqué tant que le sens du résultat AF n'est pas vérifié sur le 4000D. Le mode MF contrôlé est la voie de recette disponible. L'adaptateur EDSDK optionnel exige un SDK officiellement obtenu et une validation distincte.

Le stockage, les quotas, les secrets et les réglages de génération ne sont pas modifiés par cette revue. Les benchmarks du PC cible et la conformité physique Canon/DS620 restent à compléter lors du branchement. Ne pas présenter une simulation comme une validation matérielle.
