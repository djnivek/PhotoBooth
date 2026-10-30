# Reprise sur Windows

## PC sans caméra ni imprimante — ce soir

Télécharger [Estell’Art 0.2.1 Windows x64](https://github.com/djnivek/PhotoBooth/releases/tag/v0.2.1)
avec un compte ayant accès au dépôt privé. Extraire **tout** le ZIP dans un dossier local avant
`EstellArt.exe` ; ne pas lancer depuis la vue ZIP du navigateur. Pas de SDK .NET requis.
Le ZIP est accompagné de son SHA-256. Il a été extrait et exécuté sur Windows Server 2022 ;
[preuves et captures](evidence/windows-0.2.1/README.md). Le lancement sur le Windows 10 cible reste à vérifier.

1. Lancer en répétition (défaut). Tester 1/4 poses, Au naturel/Surprise IA simulée, placement,
   décompte 5 secondes, effet de capture, aperçu. Aucun appareil ni Internet requis pour ce parcours.
2. Réglages → choisir des photos locales autorisées ; elles sont lues dans l’ordre et répétées
   si le jeu contient moins de photos que la séance. Ce n’est pas une capture Canon.
3. Tester l’impression : export `print-preview.jpg`, aucun envoi à une imprimante. Comparer à
   l’aperçu et au JPEG de planche ; quatre photos distinctes et correctement orientées.
4. Définir un code de sortie 6–12 chiffres, activer le plein écran ; mauvais/bon code, F11,
   Échap et fermeture. Les raccourcis système restent possibles.
   « Voir les fichiers » demande aussi ce code. Après un code correct, la borne reste déverrouillée :
   retourner dans Réglages → Activer le plein écran avant de la rendre aux invités.
5. Ajouter la clé OpenAI dans Supabase, puis le code borne distinct dans les Réglages de l’app.
   Activer la connexion vérifie seulement l’authentification ; cela ne génère aucune image.
6. Faire volontairement un premier essai Surprise : **cet essai sera facturé**. Vérifier attente,
   identité des personnes, résultat et téléchargement ZIP depuis le téléphone. Puis tester quatre.
   Les originaux sont dans `originaux/` et la planche dans `planche-4x6.jpg`.
7. Vérifier le stockage Supabase et l’usage OpenAI avant de répéter à grande échelle.

Le mode Original conserve impression/aperçu même sans réseau ; un partage interrompu peut être
réessayé tant que la séance reste à l’écran. Une erreur IA permet de choisir explicitement
« Garder les originaux ». Ne pas créer une nouvelle séance pour relancer une génération incertaine.

## Photomaton — demain

Matériel demandé : Windows 10 Pro 22H2, EOS4000D USB, DNP DS620, papier PR4×6.
Relever l’architecture x64, version DSLR Booth, firmware, ports, écran et éclairage.
Le script `scripts/windows-preflight.ps1` effectue des lectures ; il ne remplace pas un essai.

Canon : cocher « Utiliser le Canon USB » et laisser la connexion avancée SDK décochée.
Cette voie utilise le pilote Windows existant via WPD/PTP ; pas de téléchargement Canon requis.
Fermer DSLR Booth/EOS Utility avant la connexion ; ne pas changer le pilote USB avec Zadig.
La compatibilité EOS4000D n’est pas encore prouvée : tester placement réel, visée, décompte,
transfert JPEG, retour visée et quatre poses. Le bouton attend une première image de visée.
Vérifier JPEG haute qualité, alimentation, veille, flash et mise au point (mise au point
manuelle à distance fixe préférable pour cette première recette, à vérifier sur les sujets).
Tester aussi Annuler pendant la connexion, une fois appareil éteint/débranché. Une connexion
native qui ne rend jamais la main peut laisser « Annulation en cours… » affiché : vérifier la
sortie par code puis le redémarrage. Ne pas relancer des connexions en parallèle ni continuer la
recette si le pilote reste bloqué ; garder DSLR Booth opérationnel et conserver le diagnostic.

Si la liaison WPD ne fonctionne pas, conserver l’erreur et la version DSLR Booth. L’adaptateur
EDSDK optionnel nécessite le SDK officiel non fourni ; ses DLL ne sont pas extraites de DSLR Booth.
Ne pas dégrader l’installation existante pour tenter de faire fonctionner la nouvelle.

DNP : installer le pilote officiel si absent, sélectionner la **bonne file DS620**, 4×6,
une copie, pas de double bande 2×6. Activer l’envoi réel dans Réglages seulement sur ce PC.
Imprimer une mire puis la planche quatre poses : une feuille, quatre images dans l’ordre,
aucune rotation inattendue, marges visibles, couleurs et visages lisibles. Examiner le papier.

Tester double appui, imprimante hors ligne et annulation Windows. « Envoyé » avec un numéro de
travail signifie accepté par le spooler, pas imprimé. Si `print.json` indique `needs_operator`,
vérifier la file avant toute reprise manuelle ; ne pas supprimer le journal pour forcer un renvoi.

## Validation finale

1. Original1, Original4 : vraies captures et feuille facultative.
2. IA1, IA4 : style stable, quatre poses distinctes, originaux + résultats téléchargés.
3. Quitter sans imprimer : aucune feuille envoyée.
4. Plein écran/code, perte réseau, redémarrage, espace libre et consommables.
5. Quelques séances consécutives avec le flash réel ; noter l’échauffement et la latence observée.

Ne déclarer le photomaton prêt qu’après ces essais. Les tests Mac/CI ne simulent pas un papier,
un capteur, un pilote ou un port USB réel. Garder DSLR Booth opérationnel pendant la recette.
