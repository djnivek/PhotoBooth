# Infrastructure — reçu de préparation

Création le 30 octobre 2026, dans le périmètre autorisé par Kevin.

| Ressource | État observé |
| --- | --- |
| GitHub | [djnivek/PhotoBooth](https://github.com/djnivek/PhotoBooth), dépôt privé |
| Organisation Supabase | [PhotoBooth](https://supabase.com/dashboard/org/scludlpexfemjustncsc), `scludlpexfemjustncsc` |
| Projet | [PhotoBooth](https://supabase.com/dashboard/project/frkckrvsdsiwivdfyjau), `frkckrvsdsiwivdfyjau` |
| Région | `eu-west-3`, Paris |
| Création | Instance `nano` demandée, statut `ACTIVE_HEALTHY` relu via CLI |
| Offre | Organisation nouvelle créée sans souscription payante ; détail de facturation non accessible via le MCP actuel |
| OpenAI | Aucun secret installé ni appel image effectué ; `xhigh` autorisé par Kevin |
| Base métier, buckets photos, file de travaux | Pas encore créés ; contrat préparé |
| Galerie et QR publics | Pas encore déployés |

Le connecteur Supabase de cette session est limité aux organisations déjà autorisées :
les lectures de coût/organisation et la création de projet sur la nouvelle organisation
retournent « You do not have permission to perform this action ». Le CLI déjà authentifié
a permis de créer l’organisation et son projet Nano. L’accès navigateur est désactivé dans
les réglages Work. Ces limitations d’accès ne sont pas des erreurs de l’application.

Pour la suite, utiliser le CLI autorisé sur ce projet ou reconnecter le plugin en incluant
PhotoBooth. Vérifier l’affichage Free/Nano dans le tableau de bord avant activation de
consommation. Aucun forfait Pro ni service payant supplémentaire n’a été souscrit.

Le mot de passe initial de base a été généré aléatoirement et conservé uniquement dans
`.local/supabase-db-password`, fichier mode 0600, exclu de Git. Il n’est pas dans les logs,
le code ou le dépôt distant. Le transférer par un canal de secrets si une connexion SQL
directe devient nécessaire sur Windows ; ne pas copier `.local` dans le dépôt.

## API de préparation

Seul `GET /health` doit être disponible publiquement sur `photobooth-api`. Il expose
uniquement version et indicateurs de préparation ; aucune photo, aucune génération,
aucune donnée utilisateur. Le déploiement et son contrôle HTTP sont consignés dans `VALIDATION.md`.

URL : `https://frkckrvsdsiwivdfyjau.supabase.co/functions/v1/photobooth-api/health`

L’absence de contrôle JWT est limitée à cette surface sans données. Avant d’implémenter
une route métier, ajouter authentification de borne, autorisation, quotas et tests ;
ne pas transformer ce point public en proxy OpenAI anonyme.

## Autorisations déjà reçues

- Dépôt privé PhotoBooth, documentation et préparation technique.
- Nouvelle organisation Supabase dédiée, démarrage gratuit.
- Mode IA **Extra High (`xhigh`)**, consommation OpenAI autorisée pour le projet.
- Kevin estime environ 50 USD pour 600 tirages et pense disposer d’environ 200 USD de crédits.
  Montants déclaratifs, pas un solde ni un devis vérifié. Un tirage peut représenter une ou
  quatre éditions ; cette préparation ne consomme pas les crédits.
