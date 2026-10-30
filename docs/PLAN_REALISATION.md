# Plan de livraison personnel

L’objectif actif est l’anniversaire du 31 octobre 2026, pas la distribution d’un produit.
Le précédent chiffrage de plusieurs semaines concernait une borne durable et une flotte :
il ne doit pas servir à retarder la répétition ni à prétendre que le matériel est déjà validé.

Complexité restante : moyenne pour le paquet et la configuration, élevée pour les inconnues
Canon/driver/Windows exact. Les quatre modes, la composition, les fichiers et la sécurité des
reprises sont déjà couverts par les tests locaux. Voir [PROGRESS.md](PROGRESS.md) pour les PR.

1. Finir API/partage et conservation originaux + résultats, tests sans facturation.
2. Publier le paquet Windows x64 autonome après exécution de ses diagnostics sur CI Windows.
3. Ce soir : test sur PC sans matériel, clé serveur, un essai IA volontaire puis QR téléphone.
4. Demain : boîtier et DLL officielles, visée/capture, DS620, mire et feuille quatre poses.
5. Après l’événement : récupérer les photos, décider de la conservation et du nettoyage.

Prérequis matériels déjà connus : Windows 10 Pro 22H2, EOS4000D, DS620, papier 4×6.
Éléments encore inconnus : architecture exacte du PC (x64 attendu), version DSLR Booth,
accès Canon SDK, éclairage/flash, marges du pilote et connexion Internet sur place.
Ne pas redemander les choix 1/4, original/IA, xhigh ni l’autorisation de merger les PRs.

Pas de modification de l’installation DSLR Booth ; garder ce logiciel fonctionnel comme
solution de capture existante. Pas d’achat, changement de forfait ou migration Windows implicite.
