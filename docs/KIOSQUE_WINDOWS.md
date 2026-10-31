# Verrouiller la borne

Le plein écran de Colors Party masque les bordures et la barre des tâches, garde la fenêtre au premier plan et refuse fermeture, réduction, Échap et F11. Sous Windows, un filtre temporaire bloque les touches Windows, Alt+Tab, Alt+Échap, Alt+Espace, Alt+F4, Ctrl+Échap et Ctrl+Maj+Échap quand la borne ou ses dialogues ont le focus. Il ne journalise pas les frappes. Il est retiré dès la sortie autorisée du plein écran et à la fermeture du processus. Aucun registre ni paramètre de sécurité Windows n'est modifié par l'application.

Maintenir trois secondes dans le coin inférieur gauche ouvre les réglages. Un code enregistré doit être saisi ; sans protection enregistrée, les réglages s'ouvrent directement. Un appui court, un déplacement hors du coin, un code incorrect ou une annulation ne déverrouille rien. Le code des réglages est distinct du code de connexion Supabase. Choisir **Quitter le plein écran** dans les réglages pour revenir à la fenêtre. Le choix plein écran est conservé au prochain lancement ; `ColorsParty.exe --kiosk` le force aussi au démarrage.

## Protection de Windows

Ce filtre d'application n'est pas une frontière de sécurité Windows. Il ne bloque pas le bureau sécurisé Ctrl+Alt+Suppr, une extinction matérielle, un administrateur ou tous les gestes système. Pour empêcher un invité d'utiliser Windows, configurer **un compte local standard dédié** avec Assigned Access, en gardant un autre compte administrateur protégé pour l'opérateur. Ne pas utiliser la session personnelle de Kevin. Sur Windows Pro, choisir l'expérience restreinte avec une liste d'applications autorisées contenant uniquement Colors Party ; le kiosque simple des Paramètres n'accepte pas cette application Win32 comme une application UWP.

Préparer et tester le Canon et la DNP depuis le compte standard avant de le restreindre. Installer l'application et ses notices dans un emplacement fixe administré, par exemple `C:\PhotoBooth`, non modifiable par les invités. Les réglages, photos et secrets protégés appartiennent au profil Windows : entrer le code de connexion depuis ce compte, et non copier le fichier DPAPI d'un autre PC. Le pilote d'imprimante doit être accessible à ce compte. Les fichiers générés restent dans son dossier local.

Le générateur fourni produit seulement un XML à relire. Il ne crée pas de compte et n'applique aucune politique :

```powershell
./New-KioskConfiguration.ps1 -Account 'NOMDUPC\PhotoBooth' -ApplicationPath 'C:\PhotoBooth\ColorsParty.exe' -TargetWindows Windows10 -OutputPath '.\AssignedAccess.xml'
```

Choisir `Windows11` uniquement à partir de Windows 11 22H2. Le profil lance Colors Party avec `--kiosk`, masque la barre des tâches et ne donne accès ni à l'Explorateur ni à un terminal. L'installation réelle peut nécessiter des autorisations complémentaires pour un composant de pilote : les ajouter seulement après identification et test.

Appliquer le XML sur le **PC de la borne** via un package de provisionnement ou le fournisseur WMI MDM, selon la procédure Microsoft. La méthode WMI exige une console **LocalSystem**, pas seulement une console administrateur. Avant application, sauvegarder toute configuration Assigned Access existante et vérifier l'accès au compte administrateur de secours. Ne pas appliquer le profil au compte de développement. Le XML généré ne constitue pas une preuve de verrouillage sur la machine de la salle.

Recette sur la borne : ouvrir la session restreinte, vérifier le lancement plein écran ; essayer fermeture, Alt+Tab, raccourcis Windows, Ctrl+Maj+Échap, balayages depuis les bords et Ctrl+Alt+Suppr ; vérifier qu'aucun bureau utilisable ni Gestionnaire des tâches n'est accessible aux invités ; tester le maintien puis mauvais/bon code ; prendre, partager et imprimer une séance ; redémarrer et recommencer. L'accès de maintenance Windows se fait avec le compte administrateur distinct. Le code Colors Party ne déverrouille pas les politiques du système.

Références Microsoft : [Assigned Access](https://learn.microsoft.com/en-us/windows/configuration/assigned-access/), [profil XML](https://learn.microsoft.com/en-us/windows/configuration/assigned-access/configuration-file), [application et suppression de la configuration](https://learn.microsoft.com/en-us/windows/configuration/assigned-access/configure-multi-app-kiosk), [politiques appliquées](https://learn.microsoft.com/en-us/windows/configuration/assigned-access/policy-settings). Shell Launcher et Keyboard Filter sont réservés aux éditions Enterprise, Education et IoT compatibles ; ils ne sont pas activés par ce paquet.
