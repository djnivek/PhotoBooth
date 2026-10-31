# Accès aux réglages et matériel par défaut, 0.10.1

Les captures `home-settings-fullscreen-1280.png` et `home-settings-fullscreen-960.png` montrent la roue crantée de 56×56 en bas à gauche de l’accueil en plein écran. Ce sont des rendus Avalonia headless avec un profil de répétition explicite. `production-default-settings.png` montre un profil neuf avec Canon et impression réelle cochés ; aucune caméra ni imprimante ne sont utilisées par cette capture.

Le parcours automatisé touche réellement la surface du bouton, puis couvre code incorrect, annulation, bon code, accès sans code et préférence plein écran après redémarrage. Le bouton du mode fenêtre demande aussi le code sauvegardé. La migration d’un profil antérieur active le matériel tout en conservant connexion, imprimante, focus confirmé, plein écran et projection ; un choix de répétition enregistré après migration survit au redémarrage. Les sélections DNP absente, unique, renommée, DS620A, explicite et ambiguë sont vérifiées avec des listes injectées, sans spooler d’impression.

Un clic de pointeur Windows a aussi ouvert la vraie fenêtre des réglages depuis une instance plein écran isolée sur le PC de développement, sans code configuré. Les deux cases Canon et impression étaient cochées. Aucun dialogue d’authentification réel n’a été automatisé ; la protection par code est vérifiée dans les profils de test. Cette vérification ne prouve ni l’USB Canon, ni la sortie papier DNP, ni les politiques de kiosque Windows.

`validation.json` rapporte les contrôles locaux. Le détecteur Impeccable n’a pas signalé de défaut. Le paquet de la release est vérifié séparément par la CI et après téléchargement ; ses reçus portent le commit exact fusionné.
