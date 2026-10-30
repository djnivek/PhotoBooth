# Handoff actif — Estelle / PhotoBooth

Lire d’abord `PROGRESS.md`. Ne pas repartir de l’ancien chiffrage de 3–6 semaines : Kevin a
précisé qu’il s’agit d’un anniversaire demain et demande une livraison personnelle ciblée.

## Machine et dépôts

- Sources : dépôt privé `djnivek/PhotoBooth` ; dossier Mac `/Users/kevinmachado/PhotoBooth`.
- Backend : projet Supabase PhotoBooth `frkckrvsdsiwivdfyjau`, organisation dédiée.
- Le CLI Supabase a accès ; le MCP de la session initiale n’avait pas accès à la nouvelle organisation.
- Aucun secret dans Git. Mot de passe DB initial conservé dans `.local/`, pas à recopier dans une archive publique.

## Architecture en cours

Le cœur .NET existe. Le nouvel hôte prévu est **Avalonia natif** pour tester le même parcours
sur Mac puis livrer Windows x64 autonome. Cette décision remplace le WPF proposé dans le cadrage.
Windows 10 reste la cible matérielle ; distinguer fonctionnement testé et support éditeur.

Le mode de test doit fonctionner sans périphérique et sans OpenAI : fixtures identifiables,
compteur, aperçu individuel, planche réelle, demande d’impression simulée. Un test réel API
sera possible par action explicite séparée, jamais depuis la suite automatique.

## Reprise de travail

1. `git status`, lire PR ouvertes et `PROGRESS.md` ; conserver tout travail en cours.
2. Lire `PRODUCT.md` et `DESIGN.md` lorsqu’ils existent ; interface Impeccable dédiée à Estelle.
3. Continuer le lot actif ; PR cohérente, validation, merge autorisé, retour sur main propre.
4. Mettre à jour ce document avec commandes, artefacts, hash et limites réelles.

## Passage au PC

À compléter lors du packaging avec le chemin exact du ZIP et la version. Il faudra extraire
tout le dossier puis lancer l’exécutable, sans installer .NET. Une archive n’est validée sur
Windows qu’après exécution réelle. Une build croisée sur Mac reste un candidat Windows.

## Passage au photomaton

Installer les dépendances et vérifier le matériel avec l’opérateur. Ne pas désinstaller
DSLR Booth. Fermer les applications qui possèdent la caméra avant ouverture par PhotoBooth.
Ne pas copier les DLL Canon depuis DSLR Booth ; utiliser un SDK acquis légalement.
Renseigner la DS620 exacte et confirmer une mire puis une planche 2×2 sur 4×6.

## Autorisations

Créer, ouvrir et merger les PRs ; préparer l’application et le paquet. Pas de génération
OpenAI pendant les tests automatiques. Kevin testera l’appel réel ce soir. Aucun achat ou
changement payant de Supabase sans chiffrage préalable. Le compte à rebours peut être animé,
mais les états « capture », « génération » et « impression » doivent refléter les vrais événements.

## Traitement photo disponible

`PhotoBooth.Imaging` produit maintenant le JPEG 1800×1200 à 300 dpi. Les huit orientations EXIF sont appliquées une fois ; les poses restent entières avec des marges si leurs proportions diffèrent. `SessionStore` écrit atomiquement et vérifie le SHA-256 avant impression. Le simulateur caméra accepte un jeu de fichiers locaux. 89 assertions supplémentaires passent sur Mac ; aucun appel payant ni impression physique. Les deux planches de calibration sont régénérées par les tests dans `artifacts/validation/`.

## Interface exécutable — PR 3

`dotnet run --project src/PhotoBooth.App` lance Estell’Art (Avalonia 12.1.4). Mode répétition par défaut : images de calibration distinctes ou fichiers sélectionnés dans Réglages, 1/4 poses, 5 secondes avant chaque capture, effet local explicitement simulé, aperçu JPEG, bouton Tester l’impression qui enregistre les mêmes octets sans spouler. Réglages permet un code 6–12 chiffres puis le plein écran ; F11/Echap/fermeture demandent le code. Le sel, le hash et le verrouillage temporaire sont persistés, jamais le code.

Sur Windows les données iront dans `%LOCALAPPDATA%\EstellArt`; aucune donnée personnelle dans le dépôt. `PHOTOBOOTH_DATA_DIR` permet aux tests d’utiliser un dossier temporaire isolé. Le test UI rend le vrai logiciel à 1280×800 et 1024×768, puis clique jusqu’au résultat et au fichier d’impression. Captures conservées dans `.impeccable/review/`. Pas de validation tactile/Windows ni matérielle impliquée.

Revue Impeccable indépendante : correction du voile de capture résolue, verdict ship limité à la répétition native. Clé OpenAI, partage et matériel restent à raccorder dans la prochaine livraison ; les messages UI le signalent.

## Adaptateurs Windows — PR 4

`PhotoBooth.Windows` fournit l’adaptateur Canon EDSDK 13.x x64 (thread STA unique, pompe d’événements, live view suspendue avant déclenchement, réception du JPEG complet, délai de 35 s sans redéclenchement) et l’impression GDI native. Le pilote est interrogé pour choisir le 4×6, une seule copie et une seule page. La surface imprimable et les résolutions X/Y sont utilisées séparément, notamment 300×600. Le raster BGRA conserve l’ordre et les couleurs du JPEG.

Avant le premier essai Canon : obtenir légalement le SDK, copier ses DLL x64 et dépendances dans `canon/` à côté de l’exécutable, installer le runtime Visual C++ requis par cette version, régler le boîtier en JPEG haute qualité, fermer DSLR Booth et EOS Utility. Les déclarations ABI doivent être confrontées aux en-têtes du SDK effectivement fourni ; aucune DLL ni aucun Canon disponible ici pour le vérifier. Cette liaison est codée, pas validée sur appareil.

Réglages permet de choisir Canon ou simulation, puis l’imprimante et l’envoi réel indépendamment. L’intention est écrite sur disque avant le premier appel au spouleur. Une erreur laisse `needs_operator` ; pas de second envoi automatique, contrôler la file Windows. Un numéro de travail prouve l’acceptation par la file, pas la sortie papier. L’impression réelle n’a jamais été lancée pendant les tests.

18 vérifications de préparation impression couvrent pixels BGRA, résolution anisotrope, zones imprimables, journal avant effet externe, succès et échec ambigu. Le test UI couvre aussi le verrouillage plein écran, la fermeture interceptée et la sortie par code.
