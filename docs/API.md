# API déployée

Base : `https://frkckrvsdsiwivdfyjau.supabase.co/functions/v1/photobooth-api`.
Le contrat [OpenAPI](../contracts/openapi.json) décrit cette implémentation personnelle.

| Méthode et route | Protection | Rôle |
| --- | --- | --- |
| GET /health | Public | Version et disponibilité de configuration, pas un test payant |
| GET /booth/check | Code borne | Vérifier la connexion et la présence de la clé OpenAI |
| POST /sessions | Code borne | UUID client, 1/4 poses, original/surprise ; création idempotente |
| PUT /sessions/{id}/assets/{name} | Borne propriétaire | Original, résultat, planche ou ZIP ; SHA-256 immutable |
| POST /sessions/{id}/photos/{index} | Borne propriétaire | Lancer une édition de l’original déjà téléversé ; JSON `{}` |
| GET /sessions/{id}/photos/{index} | Borne propriétaire | État distant et URL signée du résultat ; aucune nouvelle édition |
| POST /sessions/{id}/share | Borne propriétaire | Publier après présence de tous les fichiers requis |
| POST /downloads/resolve | Jeton de partage secret | URLs temporaires pour la page mobile préparée |

L’impression est locale et n’a aucune route Internet. Le mode Original n’appelle pas OpenAI.
Le gateway JWT est désactivé parce que l’API valide son propre Bearer aléatoire ; il ne s’agit
pas d’un proxy anonyme. Le code borne est réservé aux créations/mutations, le jeton QR donne
uniquement accès aux téléchargements d’une séance.

## Fichiers et enchaînement

`photo-1.jpg` à `photo-4.jpg`, `original-1.jpg` à `original-4.jpg` (séance IA), `sheet.jpg`,
`archive.zip`. Index de génération de 0 à 3 ; noms photo de 1 à 4. JPEG limités à 10 Mio,
ZIP à 50 Mio. Le serveur vérifie signature binaire, taille et SHA ; le client décode les images
et contrôle dimensions/orientation. Les chemins sont construits par le serveur, jamais libres.

1. Créer/retrouver la séance (UUID + mêmes options). Style persistant tiré au sort.
2. Pour IA : téléverser chaque original, puis demander son édition. Le serveur transmet à
   OpenAI une URL signée de cet original, sans nouvel upload depuis le PC.
3. Interroger le même travail. États : queued, starting, running, ready, failed, uncertain.
4. Composer et vérifier la planche localement. Téléverser résultats/planche/ZIP.
5. Publier avec un jeton aléatoire de 32 octets. Retour : ZIP signé jusqu’à l’expiration de séance.

Le client journalise les originaux déjà envoyés et les appels image tentés. La réservation
SQL est transactionnelle, unique par séance/pose, avec plafond de 2400 images. Une interruption
ambiguë ne remet jamais un travail en attente. Les uploads répétés avec un contenu identique
sont acceptés ; un autre SHA pour le même fichier retourne 409. Partage idempotent avec le même
jeton ; une séance reste liée au hash du code borne avec lequel elle a été créée.

## Secrets, erreurs et conservation

`OPENAI_API_KEY` et service_role restent côté Supabase. Le code borne est protégé via DPAPI
sur Windows ; ne pas l’inclure dans le paquet. Sur Mac de développement le fichier est mode0600,
mais son encodage local n’est pas un chiffrement et ne doit pas être transféré comme une clé Windows.
Le token de partage est hashé en base ; les réponses sensibles sont no-store/no-referrer.
Les erreurs exposent `code` et `retryable:false`, sans réponses brutes du fournisseur ni secrets.

La séance expire après 7 jours. Les URLs d’image intermédiaires expirent après 5 minutes ;
l’entrée OpenAI après 30 minutes ; le ZIP partagé après la durée restante de la séance.
Pas de purge automatique, pas de révocation immédiate d’un ZIP déjà signé : son URL reste
valable jusqu’à expiration, sauf suppression de l’objet. Pas de listing public du bucket.

## Catalogue et choix (0.3.0)

`GET /styles` est public et renvoie la version et les métadonnées d’affichage, sans prompts. `POST /sessions` accepte `style` : un identifiant du catalogue ou `chef` (défaut). Le serveur conserve choix, univers tiré et scène pour les poses de cette séance. Même UUID avec un choix différent donne 409. Les anciens clients restent compatibles. Voir [UNIVERS](UNIVERS.md).
