# Reprise sur Windows

## PC sans caméra ni imprimante — ce soir

Télécharger [le dernier paquet Colors Party Windows x64](https://github.com/djnivek/PhotoBooth/releases)
avec un compte ayant accès au dépôt privé. Extraire **tout** le ZIP dans un dossier local avant
`ColorsParty.exe` ; ne pas lancer depuis la vue ZIP du navigateur. Pas de SDK .NET requis.
Le ZIP est accompagné de son SHA-256. Le pipeline extrait le paquet et l'exécute sur Windows Server 2022 ;
[revue et reçus 0.9.0](reviews/technical-readiness-2026-10-31/README.md). Le lancement sur le Windows 10 cible reste à vérifier.

1. Le démarrage utilise le matériel réel par défaut. Pour cet essai sans matériel, ouvrir la roue crantée en bas à gauche de l’accueil, désactiver Canon USB et l’envoi à l’imprimante, puis enregistrer. Tester 1/4 poses, Au naturel/Métamorphose, placement,
   décompte 3 secondes, effet de capture, aperçu. Aucun appareil ni Internet requis pour ce parcours.
2. Réglages → choisir des photos locales autorisées ; elles sont lues dans l’ordre et répétées
   si le jeu contient moins de photos que la séance. Ce n’est pas une capture Canon.
3. Appuyer sur « Imprimer » avec l’envoi à l’imprimante désactivé : export `print-preview.jpg`, aucun envoi à une imprimante. Comparer à
   l’aperçu et au JPEG de planche ; quatre photos distinctes et correctement orientées.
4. Définir un code facultatif de quatre caractères, activer le plein écran puis fermer les réglages.
   Cliquer la roue crantée de l’accueil : annulation, mauvais/bon code, accès direct si aucun code. Tester F11,
   Échap et fermeture : aucun ne doit sortir ni ouvrir le code. Tester Alt+Tab, Alt+F4 et Ctrl+Maj+Échap. Pour restreindre Windows lui-même, suivre KIOSQUE_WINDOWS.md sur un compte standard dédié.
   « Voir les fichiers » est masqué en plein écran. Après un code correct, les réglages s’ouvrent
   sans quitter le plein écran ; les fermer suffit pour revenir aux invités.
5. Ajouter la clé OpenAI dans Supabase, puis le code borne distinct dans les Réglages de l’app.
   Activer la connexion vérifie seulement l’authentification ; cela ne génère aucune image.
6. Faire volontairement un premier essai Métamorphose : **cet essai sera facturé**. Vérifier attente,
   identité des personnes, résultat et galerie QR sur le téléphone : original et métamorphose, téléchargement individuel et ZIP. Puis tester quatre.
   Les originaux sont dans `originaux/` et la planche dans `planche-4x6.jpg`.
7. Avec le cloud activé, couper Internet et refaire une/quatre photos Au naturel. Vérifier aperçu et Imprimer, puis rétablir le réseau et Réessayer le partage sans quitter le résultat.
8. Réglages → Actualiser les univers : vérifier un ajout ou retrait backend. Ouvrir le sélecteur
   plusieurs fois ne doit afficher aucun chargement. Tester aussi sans réseau.
9. Vérifier le stockage Supabase et l’usage OpenAI avant de répéter à grande échelle.

Le mode Original conserve impression/aperçu même sans réseau ; un partage interrompu peut être
réessayé tant que la séance reste à l’écran. Une erreur IA permet de choisir explicitement
« Garder les originaux ». Ne pas créer une nouvelle séance pour relancer une génération incertaine.

## Photomaton — demain

Matériel demandé : Windows 10 Pro 22H2, EOS4000D USB, DNP DS620, papier PR4×6.
Relever l’architecture x64, version DSLR Booth, firmware, ports, écran et éclairage.
Le script `scripts/windows-preflight.ps1` effectue des lectures ; il ne remplace pas un essai.

Canon : « Utiliser le Canon USB » est déjà coché ; laisser la connexion avancée SDK décochée.
Cette voie utilise le pilote Windows existant via WPD/PTP ; pas de téléchargement Canon requis.
Fermer DSLR Booth/EOS Utility avant la connexion ; ne pas changer le pilote USB avec Zadig.
La compatibilité EOS4000D n’est pas encore prouvée : tester placement réel, visée, décompte,
transfert JPEG, retour visée et quatre poses. Le bouton attend une première image de visée.
Le résultat d’autofocus WPD n’est pas interprété comme une confirmation tant que le 4000D
n’a pas été validé. Régler l’objectif sur MF, vérifier la netteté à distance réelle, cocher
« Mise au point manuelle vérifiée sur le Canon » et enregistrer le matériel. Le logiciel
contrôle que le boîtier annonce MF ; sans cette confirmation il bloque la capture.
Vérifier aussi JPEG haute qualité uniquement (pas RAW+JPEG), alimentation, veille et flash.
Tester aussi Annuler pendant la connexion, une fois appareil éteint/débranché. Une connexion
native qui ne rend jamais la main peut laisser « Annulation en cours… » affiché : vérifier la
sortie par code puis le redémarrage. Ne pas relancer des connexions en parallèle ni continuer la
recette si le pilote reste bloqué ; garder DSLR Booth opérationnel et conserver le diagnostic.

Si la liaison WPD ne fonctionne pas, conserver l’erreur et la version DSLR Booth. L’adaptateur
EDSDK optionnel nécessite le SDK officiel non fourni ; ses DLL ne sont pas extraites de DSLR Booth.
Ne pas dégrader l’installation existante pour tenter de faire fonctionner la nouvelle.

DNP : installer le pilote officiel si absent, sélectionner la **bonne file DS620**, 4×6,
une copie, pas de double bande 2×6. Activer l’envoi réel dans Réglages seulement sur ce PC.
La notice recommande (6x4) ; PR (4x6) est son équivalent avec une orientation différente.
Avant le premier envoi, relever les formats réellement annoncés par le pilote dans PowerShell 5.1 :

```powershell
Add-Type -AssemblyName System.Drawing
$boothPrinter = New-Object System.Drawing.Printing.PrinterSettings
$boothPrinter.PrinterName = 'Nom exact de la file DS620'
$boothPrinter.PaperSizes | Select-Object PaperName, RawKind, Width, Height
$boothPrinter.DefaultPageSettings.PrintableArea
```

Ces dimensions sont en centièmes de pouce, contrairement aux dixièmes de millimètre de l'API GDI. Le format nominal donne environ 400×600 ; le raster DNP avec débord correspond à environ 413×615. Le sélecteur actuel accepte le format nominal. Si le pilote ne présente que le format élargi, conserver ce relevé pour ajuster le sélecteur et ses tests ; ne pas contourner le contrôle ni choisir un autre papier au hasard. Le refus intervient avant toute soumission.

Imprimer une mire puis la planche quatre poses : une feuille, quatre images dans l’ordre,
aucune rotation inattendue, marges visibles, couleurs et visages lisibles. Examiner le papier.

Tester double appui, imprimante hors ligne et annulation Windows. « Envoyé » avec un numéro de
travail signifie accepté par le spooler, pas imprimé. Si `print.json` indique `needs_operator`,
vérifier la file avant toute reprise manuelle ; ne pas supprimer le journal pour forcer un renvoi.
Le réglage DNP « Print Re-try » peut conserver un travail dans la mémoire de l'imprimante même s'il n'est plus dans la file Windows. Après une panne de papier, ruban ou couvercle, corriger la panne puis attendre sa reprise avant toute décision de renvoi.

À partir de 0.9.0, un refus prouvé avant `StartDoc` est journalisé `not_submitted` : corriger le réglage puis appuyer explicitement sur Imprimer est autorisé. `requested`, `submitted` et `needs_operator` restent bloquants. Aucun renvoi automatique n'a été ajouté.

## Validation finale

1. Original1, Original4 : vraies captures et feuille facultative.
2. IA1, IA4 : style stable, quatre poses distinctes, originaux + résultats téléchargés.
3. Quitter sans imprimer : aucune feuille envoyée.
4. Plein écran/code, perte réseau, redémarrage, espace libre et consommables.
5. Quelques séances consécutives avec le flash réel ; noter l’échauffement et la latence observée.

Ne déclarer le photomaton prêt qu’après ces essais. Les tests Mac/CI ne simulent pas un papier,
un capteur, un pilote ou un port USB réel. Garder DSLR Booth opérationnel pendant la recette.


## Plusieurs univers dans la version 0.9

Avant une séance de quatre poses, sélectionner de un à quatre univers, puis C’est parti ! La borne enchaîne les quatre comptes à rebours sans nouvelle question. Tous les univers choisis sont utilisés. Deux choix donnent deux images chacun ; trois choix donnent une image chacun et une répétition ; quatre choix donnent une image chacun. Les thèmes répétés occupent les diagonales de la planche. Un seul univers conserve quatre directions créatives distinctes, sans promettre des costumes forcément différents.

La Surprise du Chef est exclusive et varie les thèmes disponibles. Une photo conserve un seul choix. Les neuf univers initiaux incluent Drôles de bêtes, Sous cloche et À croquer. La grille accepte une liste plus longue et garde Retour, Chef et C’est parti ! accessibles. Pour recevoir une modification serveur, ouvrir Réglages puis Actualiser les univers. Le changement ne remplace pas les choix d’un invité en train de poser.

Au naturel reste indépendant du catalogue et du réseau. Un ancien serveur qui ne renvoie pas le plan de plusieurs univers provoque un refus avant tout envoi de photo et toute génération ; les originaux restent disponibles. Voir [la livraison multi-univers](reviews/multi-theme-2026-10-31/README.md) pour la version serveur déployée et les tests.
