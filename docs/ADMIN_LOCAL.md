# Administration locale préparée pour le Mac

Le socle est une API de rapports JSON en lecture seule. Le dashboard visuel sera réalisé sur le Mac. Rien de cet admin n'est déployé sur Vercel ou Supabase : le serveur tourne sur le Mac de l'opérateur et écoute uniquement `127.0.0.1:8787`.

Prérequis : Node.js 22+, CLI Supabase authentifiée (`supabase login`) avec accès au projet PhotoBooth. Le serveur appelle uniquement une requête SQL fixe vers `booth_monitoring_report`, sur le projet `frkckrvsdsiwivdfyjau`. Aucun mot de passe Postgres, code borne ou secret OpenAI n'est envoyé au navigateur. La CLI conserve son authentification locale.

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

## Accès privé Tailscale, à configurer sur le Mac

Définir le nom DNS Tailscale exact du Mac avant de démarrer le serveur :

```sh
export PHOTOBOOTH_ADMIN_HOST='nom-du-mac.nom-du-tailnet.ts.net'
node tools/admin-local/server.cjs
```

Puis configurer le proxy privé :

```sh
tailscale serve --bg http://127.0.0.1:8787
```

Le proxy HTTPS reste dans le tailnet et les règles d'accès Tailscale s'appliquent. Le jeton admin est toujours nécessaire. [Documentation officielle Serve](https://tailscale.com/docs/features/tailscale-serve). Aucun proxy Tailscale, serveur permanent ou exposition publique n'est lancé par les tests Windows. La commande Serve et l'accès depuis un autre appareil seront à vérifier sur le Mac.

Configuration facultative : `PHOTOBOOTH_ADMIN_PORT` (1024–65535), `PHOTOBOOTH_SUPABASE_CLI` (chemin du binaire CLI). Il n'existe pas de réglage permettant de binder le serveur sur toutes les interfaces. Arrêter avec Ctrl+C.

Tests : `node --test tests/admin.test.cjs`. Les tests utilisent un serveur local temporaire et des rapports fictifs explicitement injectés, puis ferment le port. Le test d'intégration de livraison consulte aussi le vrai rapport via la CLI, sans lancer de serveur permanent.
