# Coûts et capacité

Références Supabase vérifiées le 30 octobre 2026 ; documentation OpenAI et circuit de transfert revus le 31 octobre. À revérifier avant un changement de forfait.

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
En0.5, les fichiers individuels, la planche, le ZIP et les petites copies d’entrée IA coexistent.
Le JPEG fournisseur est directement le résultat final, sans seconde copie intermédiaire pour
les nouveaux clients. Les anciens clients conservent leur circuit compatible. Le ZIP est
assemblé côté serveur, sans compression supplémentaire des JPEG ni upload du ZIP depuis la borne.

Hypothèse de dimensionnement, **pas une mesure de vraies photos** : original2Mo, résultat
2Mo, entrée optimisée0,325Mo et planche1Mo. Pour une séance IA de quatre poses :
8Mo originaux + 8Mo résultats + 1,3Mo entrées + 1Mo planche + 17Mo ZIP = **35,3Mo**.

| Volume | Estimation selon cette hypothèse |
| --- | --- |
| 600 poses, regroupées en 150 séances de 4 | 5,3 Go |
| 600 tirages de 4 poses, donc 2400 poses | 21,2 Go |
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
après l’événement. Une optimisation future pourra enlever les copies d’entrée après
publication vérifiée ou remplacer le ZIP permanent par une galerie ; elle ne doit pas effacer
un original ni supprimer la possibilité de télécharger les quatre fichiers.

## OpenAI

La génération d’images est **comptabilisée sur le compte API OpenAI, séparément de Supabase**.
Certains abonnements ChatGPT Pro/Business peuvent fournir des crédits API après activation
du bénéfice ; ces crédits couvrent aussi Images API. Éligibilité et solde restent à vérifier.
[Crédits API OpenAI](https://developers.openai.com/api/docs/guides/chatgpt-api-credits).

Kevin demande désormais **medium**, en appel direct et carré1024×1024. Il estime
environ 50 USD pour 600 tirages et environ 200 USD de crédits disponibles ; ces chiffres
ne sont pas encore vérifiés. **600 tirages à quatre poses = 2 400 éditions** : pour tenir
50 USD, il faudrait environ 0,0208 USD par édition tout compris. À une seule pose par tirage,
le seuil serait 0,0833 USD. Vérifier l’usage medium mesuré avant de reprendre cette estimation.

Configuration0.6 demandée le31octobre : `gpt-image-2.5-sunburst` directement via Images edits,
qualité `medium`, JPEG95, sortie1024×1024. Aucun modèle Luna ni coût de modèle principal.
Le code n'impose pas de service tier. Les modèles Flare/Sunburst affichent :
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

Aucun appel réel effectué par la suite automatisée ; Kevin avait déjà déclenché son essai et
le compteur est resté à1 pendant la validation0.5. La prochaine mesure volontaire doit utiliser
un portrait autorisé puis quatre poses si le résultat convient, pour comparer coût, fidélité
et latence. Les métadonnées privées de travail enregistrent modèles demandés, tier retourné,
usage, poids d’entrée/résultat et temps de traitement, sans secrets dans les journaux publics.

## Débit et attente

Le décompte utilise trois secondes par pose ; ajouter visée, repositionnement, transfert,
génération et aperçu. Le client0.6 autorise huit éditions indépendantes, permettant deux
séances de quatre en cours, avec deux uploads et deux téléchargements simultanés. La
migration serveur fixe huit travaux non résolus et seize départs/minute. Le budget total
2400 et le compteur sont conservés. Le test HTTP attend les huit départs avant d'autoriser
le moindre résultat ; il prouve le parallélisme client, pas le délai interne d'OpenAI.
Kevin observe environ16s en medium au Playground : repère utilisateur, pas mesure de
l'application. Le délai de quatre images parallèles est celui de la plus lente, plus les
transferts et la finalisation. Les quotas OpenAI restent applicables.

Le benchmark synthétique local de quatre images détaillées mesure8,17Mo d’originaux contre
1,28Mo de copies JPEG90 à1536px maximum (−84,3 %). L’estimation du total envoyé par la borne
selon l’ancien et le nouveau circuit passe de24,22 à9,95Mo (−58,9 %), grâce au résultat direct
et au ZIP serveur. À5Mbit/s montants constants, la seule quantité d’octets correspond à38,8
contre15,9secondes : borne théorique sans RTT, contention ou génération. Ce n’est pas une mesure
4G réelle. Voir `artifacts/optimization-0.5.0/transfer-benchmark.json`.

L'API Images directe est documentée pour une photo/un prompt. Les nouvelles éditions
n'utilisent pas Responses ni Fast mode. [Guide images](https://developers.openai.com/api/docs/guides/image-generation).

### Free ou Pro pour la soirée

La documentation ne donne pas de plafond de quatre/huit invocations concurrentes. Une même
fonction déployée peut servir plusieurs requêtes et Supabase gère les workers. La limite
100/1000 concerne le nombre de fonctions déployées, pas notre nombre de poses.
Free :150s de vie d'un worker, Pro :400s ; mémoire256Mo et CPU2s/requête dans les deux cas.
L'attente réseau OpenAI est de l'I/O, distincte du CPU. Notre timeout fournisseur110s laisse
une marge pour stocker le résultat. Ce choix ne promet pas la survie d'un appel interrompu ;
il reste incertain et n'est jamais relancé automatiquement. [Limites](https://supabase.com/docs/guides/functions/limits),
[CPU](https://supabase.com/kb/troubleshooting/edge-function-cpu-limits).

Free comprend500000 invocations/mois, Pro2millions : quatre/huit générations et leur polling
ne justifient pas seuls un changement de forfait pour cet événement.
[Invocations](https://supabase.com/docs/guides/functions/pricing).
Free convient aux essais. Pour plusieurs centaines de poses avec originaux et ZIP conservés,
le stockage1Go et l'egress5Go Free sont les contraintes probables ; Pro inclut100Go de
stockage et250Go d'egress selon la documentation actuelle. Recommandation : Pro pour la
soirée si le volume prévu est maintenu, sans achat automatique.
[Quotas](https://supabase.com/docs/guides/platform/billing-on-supabase).

La rétrogradation est immédiate. Le temps prépayé inutilisé devient un **avoir dans
l'organisation**, valable sur les factures futures, sans remboursement sur la carte ; les
surconsommations restent facturées. [Conditions](https://supabase.com/docs/guides/platform/manage-your-subscription).

Les plafonds par borne/événement/jour, la réservation transactionnelle d’un budget avant
génération, le nombre limité de reprises et l’arrêt sur erreur de quota font partie du MVP.
Les limites du tableau de bord fournisseur ne doivent pas remplacer le contrôle applicatif.

## Autres coûts

Papier/ruban DNP, éventuel accès/licence Windows pour kiosque renforcé, signature de
l’installateur, domaine/galerie et worker externe si les limites Edge ne conviennent pas.
Aucun achat, upgrade payant, changement de facturation ou engagement externe n’a été demandé
à un fournisseur pendant cette préparation.

## Activation du 31 octobre

Configuration 0.6 déployée après confirmation de Kevin : huit est un plafond partagé, pas un nombre d'images obligatoire. Une séance de quatre poses lance quatre éditions ; le rythme attendu est d'au plus une séance toutes les deux minutes. Plafonds actifs : huit travaux non résolus, seize départs/minute, 2400 images au total. Le test SQL a été annulé et le compteur est resté à 1. Les tests de partage n'ont pas appelé OpenAI.

Le projet contient environ 3,66 Mo de fichiers après ces essais synthétiques ; ce poids ne dimensionne pas des photos Canon. Aucun abonnement n'a été modifié. Pro commence à 25 USD/mois pour une organisation avec un premier projet Micro couvert, hors OpenAI et dépassements. Conserver le Spend Cap activé lors d'un passage Pro ; il couvre les éléments prévus par Supabase, pas la facturation OpenAI ni tous les postes de coût. [Tarifs](https://supabase.com/pricing), [Spend Cap](https://supabase.com/docs/guides/platform/cost-control).
