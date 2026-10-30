# Contrat API cible

La spécification [OpenAPI](../contracts/openapi.json) définit le backend à construire.
**La présence d’un chemin dans ce fichier ne signifie pas qu’il est implémenté ou déployé.**
L’état réel est dans [INFRASTRUCTURE.md](INFRASTRUCTURE.md).

## Surface

| Méthode et route | Autorité | Rôle |
| --- | --- | --- |
| `GET /health` | Public, métadonnées uniquement | État de préparation et version |
| `POST /sessions` | Borne enregistrée | Créer/retrouver une séance 1/4, original/surprise |
| `POST /sessions/{id}/uploads` | Borne propriétaire | Obtenir des destinations signées imposées par le serveur |
| `POST /sessions/{id}/complete` | Borne propriétaire | Vérifier les uploads et mettre le traitement en file ; réponse 202 |
| `GET /sessions/{id}` | Borne propriétaire | Statut, progression, résultats individuels disponibles |
| `POST /sessions/{id}/sheet-upload` | Borne propriétaire | Upload signé de la planche composée localement |
| `POST /sessions/{id}/publish` | Borne propriétaire | Vérifier planche et photos avant de rendre la séance partageable |
| `POST /sessions/{id}/share` | Borne propriétaire | Créer un accès révocable à la galerie |
| `POST /downloads/resolve` | Jeton de partage secret | Résoudre le QR en liens de téléchargement temporaires |

L’impression ne possède pas de route publique : elle est locale sur Windows.
Une API Internet « ouverte » signifie joignable par HTTPS ; les créations et générations
doivent rester authentifiées et plafonnées. Un QR n’autorise jamais à lancer une génération.

## Authentification et isolation

Prévoir une identité Supabase Auth par borne provisionnée par l’opérateur. Le serveur valide
le JWT, le compte enregistré et actif, les droits sur la séance et les plafonds. La clé
publishable seule ne suffit pas. Les visiteurs n’ont pas à créer de compte.
JWT court et refresh token protégé par Windows ; aucune clé privilégiée dans l’application.

Modèle de données prévu : `booths`, `events`, `sessions`, `photos`, `generation_jobs`,
`share_tokens`, `usage_ledger`, `idempotency_records`. UUID non prédictibles, index de
propriété, contraintes sur nombre de prises et unicité `(session_id, shot_index)` et
`(session_id, shot_index, variant)`. RLS sur toutes les tables exposées ; accès service
côté serveur seulement pour les mutations sensibles.

## Upload et finalisation

- Les chemins Storage sont générés par le serveur à partir de la borne/séance/prise, jamais
  fournis librement par le client ; bucket privé.
- URL signée à usage et durée limités, fichiers JPEG, taille maximale initiale 10 Mio par copie,
  40 Mio par séance. Vérifier taille réelle, signature binaire, décodage et dimensions.
- Vérifier le SHA-256 reçu, le nombre exact et la propriété avant la mise en file.
- Pas d’URL arbitraire à télécharger côté serveur, pour éviter qu’un client pointe vers un
  service interne. Ne pas remplacer un objet déjà validé au cours d’un autre travail.
- Un fichier téléversé n’est pas une séance finalisée. La transition et l’insertion des tâches
  doivent être atomiques ; un doublon `complete` ne génère pas un nouveau travail.
- Mode Original : zéro appel OpenAI. Mode Surprise : un style tiré au sort et persisté,
  une tâche par prise, même version de prompt. La planche reste composée par la borne.

Quand toutes les photos sont prêtes, le backend passe à `awaiting_sheet`. La borne
compose la planche, obtient un upload signé, la transfère puis appelle `publish`.
Seul l’état `ready` autorise le partage. L’état local d’aperçu/impression peut exister
avant cette publication distante, notamment hors ligne.

## Reprises et erreurs

`Idempotency-Key` obligatoire sur les mutations de borne ; portée identité/route,
empreinte du payload, résultat initial renvoyé à l’identique. Même clé avec un autre contenu :
409. Conserver ces enregistrements aussi longtemps que la reprise locale peut être effectuée.
La réponse de partage contenant un jeton doit être chiffrée si elle est conservée pour rejeu ;
la table de recherche des partages ne conserve que son hash.

Erreurs structurées : `code`, `message`, `retryable`, `request_id`. 401/403 pour identité,
404 pour une séance absente ou non accessible, 409 pour conflit d’état, 413 pour taille,
422 pour contenu invalide, 429 pour plafond/rate limit, 503 pour indisponibilité. Ne pas exposer
les prompts complets, secrets ou réponses brutes du fournisseur.

Les 429 temporaires respectent `Retry-After` avec backoff et nombre de reprises borné.
Les quotas épuisés, refus de modération, fichiers invalides et erreurs d’identité n’entraînent
pas une relance automatique inchangée. Un timeout après appel image est un résultat incertain,
pas la preuve que rien n’a été facturé.

## Partage

Exemple de forme cible : `https://<galerie>/g/#<jeton-aleatoire>`. La page envoie le jeton
dans le corps de `POST /downloads/resolve`, avec cache désactivé. Le serveur contrôle hash,
expiration et révocation avant de produire des URL signées (proposition : dix minutes).
Le jeton de partage ne contient aucune clé d’administration. Aucun index public des séances.

Les durées 7 jours cloud / 30 jours local sont des propositions à valider avant activation.
La suppression doit passer par l’API Storage pour les objets, puis par la base pour les
métadonnées. Définir la reprise d’une purge interrompue et le nettoyage des uploads abandonnés.
