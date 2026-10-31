# Administration locale sur le Mac

Le socle est une API de rapports JSON en lecture seule. Le dashboard visuel sera réalisé sur le Mac. Rien de cet admin n'est déployé sur Vercel ou Supabase : le serveur tourne sur le Mac de l'opérateur et écoute uniquement `127.0.0.1:8787`.

Prérequis : Node.js 22+, CLI Supabase authentifiée (`supabase login`) avec accès au projet PhotoBooth, et ce checkout lié au projet `frkckrvsdsiwivdfyjau`. Le serveur vérifie `supabase/.temp/project-ref` avant chaque lecture non mise en cache, puis appelle uniquement une requête SQL fixe vers `booth_monitoring_report` avec `--linked`. La commande `db query` actuelle ne prend pas `--project-ref` ; les réponses JSON sous forme de tableau de lignes ou d'objet `rows` sont acceptées. Aucun mot de passe Postgres, code borne ou secret OpenAI n'est envoyé au navigateur. La CLI conserve son authentification locale.

Depuis la racine du dépôt sur le Mac :

```sh
node tools/admin-local/server.cjs
```

Un jeton d'accès local aléatoire est créé dans `artifacts/admin-local/access-token` avec permissions 0600 sur Mac. Il n'est pas affiché dans les logs et change à chaque lancement. Ne pas le versionner ou le partager dans le chat.

Pour consulter ou exporter le rapport localement :

```sh
curl --fail -H "Authorization: Bearer $(cat artifacts/admin-local/access-token)" \
  'http://127.0.0.1:8787/api/report?days=7&fixtures=0' > artifacts/admin-local/report.json
```

`days` accepte 1 à 366 ; `fixtures=1` inclut les simulations. Les rapports sont mis en cache quinze secondes. `/health` est également authentifié. Toutes les écritures HTTP, les origines web externes et les hôtes non autorisés sont refusés. Le jeton local ouvre uniquement cette API de lecture ; il n'est pas une clé Supabase.

Pour un navigateur, ouvrir `/api/report?days=7&fixtures=0` et répondre à la demande de connexion HTTP : identifiant **admin**, mot de passe **le contenu du fichier `artifacts/admin-local/access-token`**. Le même jeton est accepté en Bearer pour les outils. Ne pas mettre le jeton dans une URL, dans Git ou dans le chat. Il change au redémarrage du serveur. Le navigateur intégré à certains outils peut afficher `ERR_INVALID_AUTH_CREDENTIALS` au lieu de la boîte de connexion ; ouvrir alors le lien dans un navigateur classique.

## Accès privé Tailscale

Définir le nom DNS Tailscale exact du Mac avant de démarrer le serveur :

```sh
export PHOTOBOOTH_ADMIN_HOST='nom-du-mac.nom-du-tailnet.ts.net'
node tools/admin-local/server.cjs
```

Puis configurer le proxy privé :

```sh
tailscale serve --bg --https=443 --set-path=/photobooth http://127.0.0.1:8787
```

Le proxy HTTPS reste dans le tailnet et les règles d'accès Tailscale s'appliquent. Le jeton admin est toujours nécessaire. Le préfixe `/photobooth` est retiré par Serve avant de transmettre la requête au serveur ; `/photobooth/api/report` atteint donc `/api/report`. Préserver les autres routes et ne jamais utiliser `serve reset` ou désactiver tout le port 443. [Documentation officielle Serve](https://tailscale.com/docs/features/tailscale-serve). Aucun proxy Tailscale, serveur permanent ou exposition publique n'est lancé par les tests Windows.

Configuration facultative : `PHOTOBOOTH_ADMIN_PORT` (1024–65535), `PHOTOBOOTH_SUPABASE_CLI` (chemin du binaire CLI). Il n'existe pas de réglage permettant de binder le serveur sur toutes les interfaces. Arrêter avec Ctrl+C.

Tests : `node --test tests/admin.test.cjs`. Les tests utilisent un serveur local temporaire et des rapports fictifs explicitement injectés, puis ferment le port. Le test d'intégration de livraison consulte aussi le vrai rapport via la CLI, sans lancer de serveur permanent.

## Service activé le 31 octobre 2026

[Rapport privé sur sept jours](https://macmini-de-kevin.tail8f2f33.ts.net/photobooth/api/report?days=7&fixtures=0). Le Mac doit rester allumé et connecté à Tailscale, comme l'appareil qui consulte le rapport. Le rapport JSON existe ; le tableau de bord visuel reste une évolution distincte.

- Service utilisateur : `local.kevin.photobooth-admin`, relancé automatiquement en cas d'arrêt et au démarrage de la session Mac.
- Commande : `/opt/homebrew/opt/node@22/bin/node tools/admin-local/server.cjs`, dossier `/Users/kevinmachado/PhotoBooth`, écoute `127.0.0.1:8787`.
- Variables : `PHOTOBOOTH_ADMIN_HOST`, `PHOTOBOOTH_ADMIN_PORT`, `PHOTOBOOTH_SUPABASE_CLI` et `PATH`. Aucun secret dans le plist.
- Configuration et sauvegarde des routes précédentes : `~/.config/tailscale-project-servers/photobooth-admin.json`.
- Logs : `~/.config/tailscale-project-servers/logs/photobooth-admin.{out,err}.log`.
- Redémarrer : `launchctl kickstart -k gui/$(id -u)/local.kevin.photobooth-admin`.
- Arrêter : `launchctl bootout gui/$(id -u)/local.kevin.photobooth-admin`. Pour le réactiver : `launchctl bootstrap gui/$(id -u) ~/Library/LaunchAgents/local.kevin.photobooth-admin.plist`.

[Vérification HTTP réelle](evidence/mac-admin/service.json) : HTTPS et certificat vérifiés depuis le Mac, rapport authentifié 200, anonyme/mauvais mot de passe 401, origine étrangère 403, écriture 405. Les autres routes sont conservées et Funnel reste désactivé. Un autre appareil du tailnet n'a pas encore été testé. Aucun appel OpenAI ni écriture métier dans cette activation.
