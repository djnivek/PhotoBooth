# API PhotoBooth

Base : `https://frkckrvsdsiwivdfyjau.supabase.co/functions/v1/photobooth-api`.
Le contrat [OpenAPI](../contracts/openapi.json) décrit l'implémentation 0.7, déployée et vérifiée sur Supabase. Voir [HANDOFF](HANDOFF.md) pour la livraison Windows et les reçus.

| Méthode et route | Protection | Rôle |
| --- | --- | --- |
| GET /health | Public | Version et disponibilité de configuration, pas un test payant |
| GET /booth/check | Code borne | Vérifier la connexion et la présence de la clé OpenAI |
| POST /sessions | Code borne | UUID client, 1/4 poses, original/surprise ; création idempotente |
| PUT /sessions/{id}/assets/{name} | Borne propriétaire | Entrée optimisée privée, original, résultat, planche ou ZIP ; SHA-256 immutable |
| POST /sessions/{id}/photos/{index} | Borne propriétaire | Éditer l’entrée optimisée déjà téléversée, ou l’original pour les anciens clients ; JSON `{}` |
| GET /sessions/{id}/photos/{index} | Borne propriétaire | État et URL ; `?transport=direct` conserve le JPEG fournisseur en galerie sans réenvoi PC |
| POST /sessions/{id}/gallery | Borne propriétaire | Créer le QR stable avant les résultats |
| POST /sessions/{id}/share | Borne propriétaire | Assembler le ZIP côté serveur si nécessaire, puis publier les fichiers complets |
| POST /downloads/resolve | Jeton de partage secret | Galerie mobile, métadonnées et URLs privées temporaires |

La prise de vue, la composition et l’impression Au naturel sont locales, même avec cloud activé et réseau coupé. Le partage QR seul attend Internet, sans bloquer Imprimer. Le mode Métamorphose appelle le serveur pour générer ; une panne conserve les originaux et propose leur impression.
Le gateway JWT est désactivé parce que l’API valide son propre Bearer aléatoire ; il ne s’agit
pas d’un proxy anonyme. Le code borne est réservé aux créations/mutations, le jeton QR donne
uniquement accès aux téléchargements d’une séance.

## Fichiers et enchaînement

`input-1.jpg` à `input-4.jpg` (copies de génération privées, absentes du résolveur public), `photo-1.jpg` à `photo-4.jpg`, `original-1.jpg` à `original-4.jpg` (séance IA), `sheet.jpg`,
`archive.zip`. Index de génération de 0 à 3 ; noms photo de 1 à 4. JPEG limités à 10 Mio,
ZIP à 50 Mio. Le serveur vérifie signature binaire, taille et SHA ; le client décode les images
et contrôle dimensions/orientation. Les chemins sont construits par le serveur, jamais libres.

1. Créer/retrouver la séance (UUID + mêmes options). Style persistant tiré au sort.
2. Pour IA : créer le QR, préparer une copie JPEG90 de 1536px maximum sans recadrage, téléverser les copies et lancer jusqu’à quatre éditions indépendantes en parallèle. Les originaux non modifiés sont archivés pendant le calcul ; ils restent distincts des copies de génération. L’URL d’entrée OpenAI est signée côté serveur.
3. Interroger le même travail. États : queued, starting, running, ready, failed, uncertain.
4. Le mode direct place le JPEG fournisseur en galerie avant son téléchargement PC. Le PC vérifie son SHA, conserve ses octets sans réencoder, puis compose et téléverse seulement la planche. Les marqueurs SHA évitent de réenvoyer les fichiers déjà confirmés. Le serveur construit le ZIP à partir des objets vérifiés, à proximité du Storage.
5. Publier avec un jeton aléatoire de 32 octets. Retour : `gallery_url` pour le QR et `download_url` pour l’archive. La galerie montre chaque paire original/métamorphose, ou les seuls originaux en mode Au naturel et après repli explicite.

Le client journalise les originaux déjà envoyés et les appels image tentés. La réservation
SQL est transactionnelle, unique par séance/pose, avec plafond de 2400 images, seize départs par minute et huit travaux non résolus simultanés. Le compteur inclut les appels incertains et ne se remet jamais à zéro automatiquement. Un interrupteur serveur permet d’arrêter les nouvelles générations. Une interruption
ambiguë ne remet jamais un travail en attente. Les uploads répétés avec un contenu identique
sont acceptés ; un autre SHA pour le même fichier retourne 409. Partage idempotent avec le même
jeton ; une séance reste liée au hash du code borne avec lequel elle a été créée.

Le client0.6 partage huit places de génération entre ses séances en arrière-plan ; les uploads et téléchargements ont chacun deux places. La place de génération est libérée avant le téléchargement du résultat pour impression.

Configuration0.6 demandée par Kevin : Sunburst direct, medium, JPEG95, 1024x1024. Les métadonnées privées enregistrent modèle, qualité, taille, poids, usage et durée. Le téléphone ne reçoit aucun secret ni identifiant fournisseur.

## Appels directs 0.6

Les nouvelles générations appellent uniquement `POST https://api.openai.com/v1/images/edits` avec `model=gpt-image-2.5-sunburst`, `quality=medium`, `size=1024x1024`, `n=1`, `output_format=jpeg`, `output_compression=95`. Le JSON contient `images:[{image_url: URL_SIGNÉE}]` et le prompt complet. Aucun modèle Luna, champ Responses, révision de prompt par un modèle principal ou service tier forcé. Le secret historique `OPENAI_RESPONSE_MODEL` n'est plus lu.

La réservation transactionnelle précède tout appel payant. `EdgeRuntime.waitUntil` garde le traitement direct actif après la réponse202. Le client continue à lire l'état en base toutes les deux secondes ; aucune connexion4G n'attend tout le calcul. Le timeout fournisseur est110s, avec une marge de finalisation avant la limite Free150s. Un travail direct encore running après180s devient incertain, sans nouvel appel payant ; un résultat déjà stocké peut être récupéré après une mise à jour d'état perdue. Une erreur HTTP4xx explicite devient failed (408 reste ambiguë), un timeout/5xx devient uncertain. Les appels GET vers Responses servent uniquement à terminer d'anciens travaux déjà munis d'un identifiant ; aucun nouveau POST Responses n'existe.

La migration0.6 fixe huit travaux non résolus et16 départs/minute pour deux séances chevauchées. Le plafond total2400, le compteur consommé et l'arrêt administrateur sont conservés. Le client partage huit places de génération ; uploads et downloads restent limités à deux chacun. Les tests bloquent tout résultat jusqu'au départ des huit requêtes de deux séances.

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

## Protection 0.3.1

Voir [SECURITY](SECURITY.md) pour les limites, la révocation, les états incertains et les tests de permissions. Le navigateur ne reçoit aucun code borne ni clé fournisseur. Son unique API est le résolveur de liens, jamais une route de génération.

## Monitoring 0.7

POST `/monitoring/booth` acquitte des lots de dix mesures du client authentifié. POST `/downloads/events` accepte uniquement les compteurs d'une galerie valide et de ses fichiers disponibles. Ces routes ne génèrent aucune photo. La table `booth_metrics` est privée et aucun accès lecture n'est donné au code borne ou aux invités. Rapports opérateur via `booth_monitoring_report` et le serveur local du Mac. [Définitions et reprise](MONITORING.md), [admin local](ADMIN_LOCAL.md).

## Projection de salle (0.8)

`POST /viewer/settings` et `POST /sessions/{id}/viewer` demandent le bearer de la borne. Les réglages enregistrent uniquement le hash d’un code viewer distinct ; la seconde route libère une séance de manière idempotente. `POST /viewer/feed` et `POST /viewer/photos` demandent ce code de lecture dans le corps JSON, sans bearer de borne. Le feed ne contient aucune URL ni clé Storage ; un hash de révision permet une réponse compacte lorsque les métadonnées sont inchangées. La route photos signe uniquement les résultats éligibles de la séance demandée. Aucun endpoint viewer ne crée ou relance une génération. Les tables et la fonction SQL restent réservées au rôle serveur. Détails : [VIEWER](VIEWER.md).
