# Tableau de bord privé sur le Mac

Ouvrir [l’espace admin Colors Party](https://macmini-de-kevin.tail8f2f33.ts.net/photobooth/), puis saisir le contenu du fichier local `artifacts/admin-local/access-token`. Sur le Mac, « Où trouver mon code ? » permet aussi de sélectionner ce fichier. Il est distinct du code borne et de la clé OpenAI. Ne jamais copier sa valeur dans Git, les logs, une URL ou le chat.

Le code reste identique après redémarrage. La connexion utilise une session de douze heures avec cookie HttpOnly, SameSite Strict et Secure sur Tailscale. Un redémarrage demande de se reconnecter, sans changer le code. « Se déconnecter » révoque la session. Cinq codes incorrects bloquent les nouvelles tentatives pendant une minute.

L’ancien lien `/api/report?days=7&fixtures=0` redirige les navigateurs vers le dashboard. Les outils continuent à recevoir du JSON et peuvent utiliser Bearer. L’ancienne acceptation Basic est conservée pour compatibilité, mais aucune boîte de connexion HTTP n’est imposée au navigateur.

## Ce que montre le dashboard

- Périodes glissantes de 24 heures, 7 ou 30 jours, essais exclus par défaut, heures affichées à Paris.
- Séances démarrées, captures sauvegardées, générations réussies, clics de téléchargement et activité par heure ou jour.
- Moyenne, médiane, p95, opérations mesurées/réussies, échecs/incertitudes et volumes de transfert.
- Modes, résultats prêts, séances échouées, consultations de galerie, images vues, types de fichiers téléchargés, univers choisis et générés.
- État actuel du serveur, distinct de la période : réservations sur le plafond d’images, limites de débit/concurrence, traitements en cours/incertains/anciens et volume connu du bucket privé.

Les indicateurs sont en lecture seule. Le plafond d’images ne représente ni la facture ni le crédit OpenAI. Le volume Stockage compte les métadonnées des fichiers du bucket `booth-private`, pas le quota du forfait ni les autres projets. Une taille absente rend ce total explicitement partiel. Un traitement sans mise à jour depuis cinq minutes est à vérifier, sans conclure à un échec ni relancer un appel.

Le tableau se rafraîchit trente secondes après la lecture précédente tant que l’onglet est visible. Une panne conserve le dernier rapport, sa date et ses filtres. Aucun zéro n’est substitué à un rapport indisponible. L’export JSON reprend les filtres sélectionnés et nécessite toujours une session autorisée. Les détails numériques du graphique sont accessibles dans « Voir les valeurs » et les tableaux se font défiler sur téléphone.

La collecte précise des séances, captures et transferts nécessite **la borne 0.7**. Les générations historiques sans source ni durée restent signalées. Les compteurs ne prouvent pas une connexion Canon, une impression physique, des visiteurs uniques ou des téléchargements terminés. Voir [MONITORING.md](MONITORING.md).

## Démarrage et architecture

Prérequis : Node.js 22+, CLI Supabase authentifiée (`supabase login`) et checkout lié au projet PhotoBooth `frkckrvsdsiwivdfyjau`.

```sh
node tools/admin-local/server.cjs
```

L’écoute est exclusivement `127.0.0.1:8787`. Le code est créé une seule fois avec permissions 0600 sur Mac. Un fichier existant invalide ou un lien symbolique fait échouer le démarrage, plutôt que remplacer silencieusement le code.

Avant chaque lecture non mise en cache, le serveur vérifie `supabase/.temp/project-ref`. La CLI exécute seulement la requête agrégée fixe `tools/admin-local/report.sql` avec `db query --linked --file … --output json`. Les paramètres sont réduits à un nombre de jours borné et un booléen. Aucun identifiant de séance, photo, prompt, clé fournisseur ou secret Supabase n’est envoyé au navigateur. Aucun appel de génération, migration ni écriture métier.

Les rapports sont mis en cache quinze secondes, quatre variantes au maximum. Les lectures simultanées identiques partagent une requête ; quatre lectures différentes au maximum sont lancées en parallèle. Les origines et hôtes étrangers sont refusés. Les seules routes POST sont la connexion et la déconnexion, protégées par origine exacte et en-tête dédié. `/health`, `/api/session` en GET et `/api/report` sont authentifiés. La page de connexion et les cinq assets explicitement autorisés ne contiennent pas de statistiques.

Pour consulter le JSON avec un outil, utiliser `Authorization: Bearer <code admin local>`, sans mettre ce code dans une URL. `/api/report?days=7&fixtures=0` accepte 1 à 366 jours ; `fixtures=1` inclut les essais. Ajouter `download=1` force un fichier JSON téléchargeable, même avec un en-tête Accept HTML.

## Service Tailscale installé

Le Mac et l’appareil de consultation doivent être allumés/connectés à Tailscale. Aucun Funnel public n’est utilisé.

- Service utilisateur : `local.kevin.photobooth-admin`, lancé à l’ouverture de la session Mac et relancé après arrêt imprévu.
- Commande : `/opt/homebrew/opt/node@22/bin/node tools/admin-local/server.cjs`, cwd `/Users/kevinmachado/PhotoBooth`.
- Écoute : `127.0.0.1:8787` ; route privée `/photobooth` vers ce port.
- Variables : `PHOTOBOOTH_ADMIN_HOST=macmini-de-kevin.tail8f2f33.ts.net`, `PHOTOBOOTH_ADMIN_BASE_PATH=/photobooth`, `PHOTOBOOTH_ADMIN_PORT=8787`, `PHOTOBOOTH_SUPABASE_CLI=/opt/homebrew/bin/supabase`, `PATH`. Aucun secret dans le plist.
- Configuration locale : `~/.config/tailscale-project-servers/photobooth-admin.json`.
- Logs : `~/.config/tailscale-project-servers/logs/photobooth-admin.{out,err}.log`.

```sh
launchctl kickstart -k gui/$(id -u)/local.kevin.photobooth-admin
```

Pour arrêter : `launchctl bootout gui/$(id -u)/local.kevin.photobooth-admin`. Pour réactiver : `launchctl bootstrap gui/$(id -u) ~/Library/LaunchAgents/local.kevin.photobooth-admin.plist`. Après un bootout, attendre que le service ait disparu avant un nouveau bootstrap ; un arrêt encore en cours peut produire une erreur 5 transitoire.

Serve retire le préfixe `/photobooth` avant transmission. Le serveur prend aussi en charge ce préfixe pour les tests locaux, et le réinjecte dans les assets, appels, redirections et chemin du cookie. Ne pas réinitialiser les autres routes Tailscale. Pour une nouvelle installation, définir le nom DNS exact et la base puis ajouter uniquement cette route :

```sh
tailscale serve --bg --https=443 --set-path=/photobooth http://127.0.0.1:8787
```

## Vérifications et preuves

```sh
node --test tests/admin.test.cjs tests/admin-model.test.cjs
```

Les tests utilisent des serveurs temporaires et des rapports fictifs, sans appel payant. Les mêmes tests font partie des scripts de vérification Mac et Windows. [Captures et preuves](evidence/admin-dashboard/README.md), [vérifications HTTPS réelles](evidence/admin-dashboard/live-checks.json) et [revue Impeccable](reviews/admin-dashboard/impeccable.md).

L’interface, la connexion par fichier, les filtres, l’export, la panne avec conservation des chiffres, la déconnexion et le rechargement de session ont été vérifiés dans le navigateur. La vraie URL Tailscale et son certificat ont été vérifiés depuis le Mac serveur, avec les données réelles. Un autre appareil physique du tailnet reste à vérifier.
