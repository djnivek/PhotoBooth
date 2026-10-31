# Monitoring 0.7 — preuves

La collecte est déployée sur Supabase et sur la galerie Vercel. Aucun rendu OpenAI payant dans ces vérifications.

- [Reçu de livraison](delivery.json) : code, paquet autonome extrait, suites et limites.
- [Collecte relue en base](live-collection.json) : quatre captures synthétiques, deux ouvertures, quatre images effectivement vues, quatre demandes de téléchargement et neuf uploads mesurés. Budget resté à 1/2400.
- [Navigateur réel](production-browser.json) : ordinateur/mobile, progression automatique du même QR puis téléchargement du JPEG et du ZIP sur chaque appareil. Le test navigateur peut vérifier sa propre réception ; la statistique produit reste un clic demandé.
- [Admin local temporaire](admin-live.json) : vrai rapport Supabase, requête sans jeton refusée, simulations exclues ; serveur fermé.
- [Assertions SQL](monitoring-assertions.sql) : dédoublonnage, filtrage, moyennes et p95, permissions ; exécutées dans une transaction annulée avant application de la migration.

Le serveur Mac et Tailscale restent à valider sur le Mac. L’interface graphique du dashboard sera ajoutée à ce socle local de rapports JSON. [Procédure](../../ADMIN_LOCAL.md), [définitions des métriques](../../MONITORING.md).
