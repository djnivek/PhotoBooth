# Monitoring PhotoBooth 0.7

Les logs techniques Supabase restent disponibles pour le diagnostic. Les statistiques métier sont conservées séparément dans `public.booth_metrics`, une table privée avec RLS, sans accès anon/authenticated. Aucun service de suivi tiers ni cookie n'est ajouté.

## Mesures et définitions

| Mesure | Ce qui est compté |
| --- | --- |
| Séance | Clic de démarrage effectif, avec mode, une/quatre poses demandées et thème choisi |
| Photos prises | Une mesure par capture JPEG réellement sauvegardée ; une séance interrompue peut avoir moins de poses que demandé |
| Images générées | Réponse OpenAI valide, distincte du succès du stockage ou de la publication ; échec/incertitude conservés séparément |
| Upload/download borne | Chaque transfert HTTP effectivement effectué, durée jusqu'au corps complet, octets et résultat ; attente des places de transfert exclue, fichiers déjà confirmés non retransférés non comptés |
| Génération | Durée de la requête OpenAI jusqu'à réception complète de la réponse, sans finalisation Storage ; le temps global précédent reste dans le journal de travail |
| Ouvertures galerie | Une par document et séance consultée, pas par polling, pas un nombre de personnes uniques ni une preuve de scan physique du QR |
| Images vues | Image chargée et entrée dans le viewport, une fois par fichier et document ; navigateur avec IntersectionObserver requis |
| Téléchargements | Clics sur une photo, une planche ou le ZIP. Le navigateur n'atteste pas la fin du transfert sur tous les appareils |

Les rapports fournissent moyenne, médiane, p95, nombre d'échantillons et échecs séparés pour chaque type de durée. Les modes sont comptés par séance démarrée ; les thèmes demandés et les thèmes effectivement générés sont distincts, notamment pour La Surprise du Chef. Les agrégats horaires bruts utilisent UTC.

Les anciennes générations déjà connues sont conservées comme historiques non classifiés, sans inventer leur durée ni le nombre de captures. La collecte précise des séances/captures/transferts exige **l'application 0.7**. Les statistiques du site et des générations commencent avec le déploiement 0.7. `collection_started_at` et les définitions sont inclus dans le rapport.

## Reprise et limites

La borne garde les mesures non acquittées dans `EstellArt/monitoring/` et envoie des lots de dix toutes les quinze secondes. Les identifiants sont conservés après une coupure ; la base ignore les doublons. Les mesures ne déclenchent jamais de prise de vue, génération, impression ou réenvoi de photo. Un échec de monitoring ne bloque pas ces opérations. Les dossiers locaux sont bornés à 20 000 mesures par API ; surveiller ce plafond si le PC reste durablement hors ligne.

Les événements de consultation sont envoyés en petits lots avec keepalive, sans attendre avant le téléchargement. Leur collecte reste au mieux possible : fermeture brutale, bloqueur ou panne réseau peuvent faire perdre des événements. Aucun identifiant visiteur persistant, adresse IP, user-agent, nom, prompt, photo, URL signée ou secret n'est stocké dans la table de mesures. Le UUID de séance permet les rapprochements ; il ne rend pas ces mesures indépendantes des séances privées. Les logs techniques des fournisseurs ont leur propre politique.

Les données d'essai portent `fixture`. Les rapports excluent par défaut les séances identifiées comme essais ; `fixtures=1` les inclut. Les données historiques dont la source est inconnue restent signalées comme non classifiées. Les écritures visiteurs sont limitées aux capacités valides et aux assets publics déjà disponibles, avec 5 000 événements maximum par séance. La borne est limitée à 1 000 mesures par séance. Les visiteurs et le code borne ne peuvent pas lire les statistiques.

La conservation est durable : pas de purge automatique de cette table, ni de suppression des originaux ajoutée par ce changement. Les mesures ajoutent des écritures et un petit volume de stockage ; aucune souscription ou option payante n'est activée. Le budget de génération reste 2 400, avec huit places et seize départs/minute.

## API et validation

- `POST /monitoring/booth` : code borne, enveloppe validée, lot de dix maximum, acquittement avant retrait du disque.
- `POST /downloads/events` : jeton de galerie dans le corps, aucun accès aux copies privées ou aux autres séances.
- `public.booth_monitoring_report(days, include_fixtures)` : fonction de lecture réservée au serveur/opérateur.

L'admin local est documenté dans [ADMIN_LOCAL.md](ADMIN_LOCAL.md). Le dashboard visuel sera ajouté sur le Mac. La commande de vérification de production publie une séance synthétique, observe la progression dans Chromium et vérifie des clics de téléchargement : `node scripts/verify-production-gallery.cjs`. Elle ne demande pas de génération OpenAI. Les tests SQL de moyenne, p95, dédoublonnage, filtrage et permissions sont dans [les preuves](evidence/monitoring/monitoring-assertions.sql), exécutés dans une transaction annulée avant application de la migration.
