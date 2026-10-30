# Coûts et capacité

Références vérifiées le 30 octobre 2026 ; à revérifier avant un changement de forfait.

## Supabase

Free annonce 500 Mo de base, 1 Go de fichiers, 5 Go de trafic sortant non mis en cache
et 5 Go de trafic en cache ; les projets peuvent être mis en pause après une semaine
d’inactivité. Deux projets actifs gratuits sont autorisés selon les conditions de compte.
Créer une nouvelle organisation ne contourne pas cette limite. Pro est affiché à partir
de 25 USD/mois. [Tarifs Supabase](https://supabase.com/pricing).

Free convient à la répétition, mais ne doit pas être considéré comme suffisant pour 600 photos.
Kevin demande maintenant la conservation cloud des originaux **et** des versions IA.
Décision du 30 octobre au soir : conserver le forfait actuel pour les essais. Réévaluer
le stockage demain uniquement après validation du fonctionnement ; aucun upgrade aujourd’hui.
Dans l’implémentation actuelle, les fichiers individuels, la planche et le ZIP coexistent ;
le résultat fournisseur intermédiaire est également conservé. Le ZIP n’est pas compressé
une seconde fois : les JPEG sont déjà compressés, et ses octets restent stables en reprise.

Hypothèse de dimensionnement, **pas une mesure de vraies photos** : original 2 Mo, résultat
2 Mo, résultat fournisseur 2 Mo et planche 1 Mo. Pour une séance IA de quatre poses :
8 Mo originaux + 8 Mo résultats + 8 Mo intermédiaires + 1 Mo planche + 17 Mo ZIP = **42 Mo**.

| Volume | Estimation selon cette hypothèse |
| --- | --- |
| 600 poses, regroupées en 150 séances de 4 | 6,3 Go |
| 600 tirages de 4 poses, donc 2400 poses | 25,2 Go |
| Originaux + résultats seuls pour 600 poses | 2,4 Go, avant copies/ZIP/planches |

Le poids réel dépend fortement du cadrage, des détails et des résultats IA. La normalisation
JPEG de travail à 2560 pixels limite le volume, sans garantir 1 Go. Les images individuelles
sont limitées à 10 Mio ; le ZIP à 50 Mio, plafond maximal des fichiers Free.
Les téléchargements par OpenAI, par la borne et par les invités comptent aussi dans l’egress.

**Recommandation pour 600 photos : Pro, à partir de 25 USD/mois avec 100 Go Storage et un
premier projet Micro couvert par les crédits compute, selon le tarif vérifié.** Ne pas upgrader
sans décision de Kevin ; aucune souscription n’a été faite. Ajouter une marge et mesurer
une vraie séance ce soir. [Stockage Supabase](https://supabase.com/docs/guides/storage/pricing).

Les liens expirent après 7 jours mais les fichiers restent stockés : décider du nettoyage
après l’événement. Une optimisation future pourra enlever les résultats intermédiaires après
publication vérifiée ou remplacer le ZIP permanent par une galerie ; elle ne doit pas effacer
un original ni supprimer la possibilité de télécharger les quatre fichiers.

## OpenAI

La génération d’images est **comptabilisée sur le compte API OpenAI, séparément de Supabase**.
Certains abonnements ChatGPT Pro/Business peuvent fournir des crédits API après activation
du bénéfice ; ces crédits couvrent aussi Images API. Éligibilité et solde restent à vérifier.
[Crédits API OpenAI](https://developers.openai.com/api/docs/guides/chatgpt-api-credits).

Kevin a autorisé la qualité **Extra High (`xhigh`)** et la consommation API. Il estime
environ 50 USD pour 600 tirages et environ 200 USD de crédits disponibles ; ces chiffres
ne sont pas encore vérifiés. **600 tirages à quatre poses = 2 400 éditions** : pour tenir
50 USD, il faudrait environ 0,0208 USD par édition tout compris. À une seule pose par tirage,
le seuil serait 0,0833 USD. Vérifier l’usage xhigh mesuré avant de reprendre cette estimation.

Les modèles candidats Flare/Sunburst affichent au moment de cette préparation :
texte entrant 5 USD/M tokens, image entrante 8 USD/M tokens, image sortante 30 USD/M tokens
en traitement standard. Le coût par photo dépend des tokens réellement consommés, de la
qualité et de la taille ; le calculateur GPT Image 2 ne prédit pas les tokens Image 2.5.
Sources : [Flare](https://developers.openai.com/api/docs/models/gpt-image-2.5-flare),
[Sunburst](https://developers.openai.com/api/docs/models/gpt-image-2.5-sunburst).

Formule indicative hors cache :

`coût édition = tokens texte entrée × 5/1e6 + tokens image entrée × 8/1e6 + tokens image sortie × 30/1e6`

Une séance Original ne demande aucune génération. Une séance IA à quatre prises nécessite
quatre éditions et coûte approximativement quatre fois une édition comparable, plus les éventuelles reprises.
Exemples de budget de sensibilité, **pas des tarifs par image garantis** :

| Coût mesuré futur par édition | Séance 1 photo | Séance 4 photos | 200 séances de 4 |
| --- | --- | --- | --- |
| 0,05 USD | 0,05 USD | 0,20 USD | 40 USD |
| 0,10 USD | 0,10 USD | 0,40 USD | 80 USD |
| 0,20 USD | 0,20 USD | 0,80 USD | 160 USD |

Pas d’appel réel effectué pendant la préparation. Ce soir : un premier essai volontaire sur
une photo, puis une série de quatre si le résultat convient, pour mesurer coût, fidélité et latence. Conserver l’usage retourné par le
fournisseur, la version du modèle et le prompt utilisé, sans journaliser les images ou secrets.

## Débit et attente

Le décompte seul prend 5 ou 20 secondes ; ajouter placement, transfert, génération et aperçu.
Avec deux éditions simultanées et une latence observée de 45 s par photo, une série de quatre
prendrait environ 90 s plus transferts : c’est un scénario, pas une mesure. Ne pas annoncer
« prêt en quelques secondes » avant le benchmark. La concurrence et les limites du compte
API doivent être vérifiées, pas déduites du nombre de visiteurs.

Les plafonds par borne/événement/jour, la réservation transactionnelle d’un budget avant
génération, le nombre limité de reprises et l’arrêt sur erreur de quota font partie du MVP.
Les limites du tableau de bord fournisseur ne doivent pas remplacer le contrôle applicatif.

## Autres coûts

Papier/ruban DNP, éventuel accès/licence Windows pour kiosque renforcé, signature de
l’installateur, domaine/galerie et worker externe si les limites Edge ne conviennent pas.
Aucun achat, upgrade payant, changement de facturation ou engagement externe n’a été demandé
à un fournisseur pendant cette préparation.
