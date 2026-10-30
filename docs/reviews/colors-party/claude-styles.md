# Creative direction review

Model: `claude-opus-5-5`. Effort: `max`. Completed 2026-10-30.

Scope: brief only, no source inspection, image viewing or hardware validation. The first broad attempt was stopped without a final report; this bounded follow-up completed.

# Revue de direction créative : Colors Party by Estelle

**Périmètre**
Je critique uniquement la direction décrite ici : les noms, le ton, les textes invités, le mouvement du loader, l'expérience du catalogue et l'accès caché. Je n'ai vu ni les emblèmes, ni les maquettes, ni les rendus, ni le code. Je tiens la gouache mate et la palette pour validées.

**1. Noms : des lieux et des fêtes, jamais des espèces**
- « Colors Party » désigne à la fois la marque et un thème. Renommer le thème « Fleurs en fête » : un décor de studio en plein jour (tissus, fleurs géantes), distinct du jardin des licornes et de la nuit disco.
- « Métamorphose » promet de changer les corps, ce que la règle d'identité interdit. Le mot sonne aussi mal quand le fauteuil d'un invité est, à raison, conservé. Proposer « En scène », avec le sous-titre « Costumes et décors changent, vous gardez la vedette. »
- « Pays imaginaire » est le nom français de Neverland : les invités attendront Peter Pan. Proposer « Jardin des licornes ».
- « Petits lutins » annonce un rétrécissement et des oreilles pointues, et son décor géant risque de faire doublon avec les fleurs. Proposer « Chez les lutins ». Si l'emblème s'y prête, « Le goûter des lutins » (gâteaux géants) ferait en plus un clin d'œil à l'anniversaire.
- Les descriptions règlent l'attente sans rien promettre. « Combinaisons rétro, casque sous le bras » justifie le visage découvert. « Capes, chapeaux et citrouilles complices » écarte les masques.
- Libeller l'option « Le choix du chef » : la métaphore cuisine des phrases devient un fil rouge.

**2. Phrases**
- « Vos sourires sont aux petits soins » est fautif : on est aux petits soins *pour* quelqu'un. Proposer « Vos sourires sont entre de bonnes mains. »
- « Les étoiles prennent la pose » se confond avec la progression réelle des poses. Proposer « Les étoiles s'alignent. »
- « Les licornes se font une place » évoque une bousculade. Proposer « Les licornes lissent leur crinière. »
- Avec une seule phrase par thème, la boucle se voit en vingt secondes environ. En écrire au moins trois par thème, sans répétition immédiate.
- Séparer trois répertoires : générique, thème et chef. En mode chef, garder le badge et les phrases du chef jusqu'au résultat. Afficher le badge du thème tiré gâcherait la surprise.
- Attention aux traits d'union cachés : « arc-en-ciel », « chauves-souris », « rendez-vous » et tout impératif pronominal. Écrire « Prenez place » plutôt que « Placez-vous ».

**3. Mouvement**
« 3 px à 12,5 Hz » ne fixe pas la période. Lu comme une fréquence, c'est une vibration. Lu comme une cadence d'image, il manque la durée du cycle, et 3 px sur un badge de 288 px vu à deux mètres passent pour un tic ou pour rien. Spécifier plutôt : 5 à 6 px, un cycle d'environ 2,5 s, une courbe sinusoïdale, à valider à distance réelle. Le reste (mouvement réduit, timers arrêtés, aucune fausse progression) est juste.

**4. Catalogue**
- Ne jamais remplacer la liste pendant qu'un invité choisit. Appliquer la mise à jour au retour à l'accueil.
- Ajouter l'ordre et la visibilité, thèmes intégrés compris, par exemple pour masquer Halloween en avril. Sinon la grille ne fait que grossir et la file ralentit.
- Illustrations de secours : jamais deux badges identiques à l'écran. Exclure aussi ces thèmes du choix du chef, sinon la révélation tombe à plat.
- Hors ligne, la liste reste visible mais aucune scène ne peut être générée. Désactiver « En scène » avant la prise de vue, plutôt que d'échouer après quatre photos.
- Soumettre les textes du catalogue aux mêmes règles que l'app : longueur maximale, aucun tiret ni trait d'union, aucune mention d'IA, aucune promesse de ressemblance.

**5. Accès caché**
Le coin bas gauche est à hauteur d'enfant et accueille souvent un bouton Retour. Il faut :
- n'y placer aucun contrôle invité ;
- activer le code par défaut ;
- refermer les réglages après une minute d'inactivité, puisqu'ils restent ouverts au passage en plein écran.

**Ruban vertical**
S'il reste, lui donner un rôle : servir de signet au choix du chef, plutôt qu'une septième case qui casse la grille. Sinon, le retirer.

**Verdict**
Direction solide, à garder. Le ton est complice sans être moqueur, et Halloween, Dans les étoiles et Disco velours fonctionnent. Corrections prioritaires avant l'événement :
- « Métamorphose » et « Pays imaginaire » ;
- la collision Colors Party ;
- la phrase « petits soins » ;
- la spécification du mouvement ;
- le gel de la liste pendant une session.

Le reste affine sans toucher à l'esprit gouache.

## Decisions

Accepted: corrected the three phrases, specified a 2.5 second sine cycle with 5 px movement, froze background catalog refresh until the next home screen, and reject server guest copy containing dashes or AI terminology. There are already three phrases per theme. Chef keeps its own art and phrases until the result.

Kept as requested: Métamorphose, Colors Party, Pays imaginaire, Petits lutins, La Surprise du Chef, the optional four-character code, and the approved artwork. Catalog array order is display order; removing an entry hides it. Unknown art uses the local fallback the user requested. No automatic settings dismissal was added. A catalog request failure does not prove the image service is down, so it does not disable the existing rehearsal or stored choices.
