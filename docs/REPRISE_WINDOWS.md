# Reprise sur le PC Windows

## Première session

1. Cloner `https://github.com/djnivek/PhotoBooth` dans un dossier de travail local.
2. Lire le README, le cahier des charges et les décisions D01/D02 avant de choisir le runtime.
3. Lancer le diagnostic en PowerShell depuis la racine :

```powershell
./scripts/windows-preflight.ps1
```

Le script affiche version Windows, architecture, mémoire, imprimantes et processus connus.
Il n’installe rien, ne ferme aucun logiciel, ne change aucun pilote, ne touche pas au registre
et ne modifie pas la politique d’exécution. Si Windows refuse le script, examiner la politique
avec le propriétaire ; ne pas la désactiver globalement.

4. Relever les réglages actuellement fonctionnels dans DSLR Booth et la DS620 ; sauvegarder
   la configuration avant tout changement. Ne pas désinstaller l’application actuelle.
5. Déterminer la version runtime utilisable et son statut de support sur ce Windows.
   Le SDK .NET 10 du Mac n’est ni un installateur Windows ni une preuve de compatibilité.
6. Installer uniquement les dépendances nécessaires depuis Canon/DNP/Microsoft.

## Canon EOS 4000D

- USB de données fiable, alimentation continue, emplacement caméra fixe, objectif identifié.
- Demander la fermeture de DSLR Booth/EOS Utility avant le test si l’opérateur les utilise.
- EDSDK sous licence, architecture des DLL égale à celle du processus ; noter version et firmware.
- Tester connexion, informations boîtier, JPEG live view et prise pleine résolution séparément.
- Vérifier la visée à l’écran, le miroir, le cadrage, le délai et le retour après déclenchement.
- Vérifier la combinaison live view, autofocus et flash réellement utilisée. Attendre la recharge.
- Tester vingt prises consécutives, une série de quatre, mise en veille, câble débranché/rebranché,
  erreur `busy`, stockage local saturé. La disparition de la visée pendant l’obturateur ne doit
  pas provoquer une deuxième prise.

## DNP DS620, 4×6

- Installer le pilote officiel correspondant au Windows exact et relever sa version.
- Identifier le nom Windows de la DS620 ; ne pas utiliser une autre imprimante par défaut.
- Lire les formats/zone imprimable exposés par le pilote, désactiver la coupe 2×6.
- Imprimer une mire de cadrage et la planche 2×2 sur une seule feuille ; mesurer les bords.
- Comparer une photo pleine page et les quatre poses, couleurs, sens, marge et absence de coupe des visages.
- Tester imprimante hors ligne, ruban/papier absent, double appui sur Imprimer, fermeture
  après soumission et reprise de Windows. Aucun doublon automatique après état incertain.
- Vérifier visuellement le papier : un job spooler terminé ne suffit pas.

## Supabase et OpenAI

Projet dédié : `frkckrvsdsiwivdfyjau`, Paris `eu-west-3`. Ne pas utiliser MatchingScore.
Voir `INFRASTRUCTURE.md` pour l’état réel et le connecteur à reconnecter si nécessaire.
Ne transférer aucun mot de passe via Git ; les secrets devront être provisionnés séparément.

1. Configurer les identités de borne, désactiver l’auto-inscription non nécessaire, puis tester
   qu’une autre borne/un visiteur ne peut lire ni écrire les séances.
2. Déployer les tables/policies/fonctions métier depuis des migrations revues, contrôler RLS.
3. Configurer les secrets côté serveur, un plafond de test et des photos autorisées.
4. Tester une édition puis quatre, panne partielle, modération, quota, timeout incertain,
   reprise sans relancer les résultats déjà sauvegardés.
5. Déployer la page mobile, scanner le QR via 4G/5G, télécharger toutes les photos et la planche.
6. Vérifier expiration/révocation, absence de listing public et purge Storage via son API,
   puis suppression des métadonnées correspondantes.

## Recette finale ordonnée

1. **Original 1** : visée → 5…1 → vraie capture → aperçu → une impression facultative.
2. **Original 4** : quatre décomptes, quatre poses distinctes, une planche, quatre téléchargements.
3. **IA 1** : un style surprise, résultat reconnaissable, impression et QR cohérents.
4. **IA 4** : même style, quatre résultats distincts dans le bon ordre, aucun mélange silencieux.
5. **Sans impression** : le visiteur télécharge et termine ; aucune feuille ne sort.
6. **Borne** : plein écran, code faux/valide, délai après erreurs, inactivité admin, reboot ;
   tester Échap/F11/Alt+F4/Alt+Tab/touche Windows/Ctrl+Alt+Suppr/gestes tactiles/écran secondaire.
   Noter les sorties système restantes avec Windows Pro.
7. **Pannes** : caméra, imprimante, réseau, disque, backend, crédits, crash à chaque étape.
8. **Endurance** : objectif initial 100 séances/plusieurs heures, mémoire stable, aucun fichier
   d’une autre séance, aucun double tirage ; suivre coût, latence et consommables.
9. **Installation** : machine de référence, démarrage après reboot, migration des réglages,
   désinstallation sans effacer implicitement les photos du propriétaire.

Conserver les résultats et versions dans `docs/VALIDATION.md`, avec captures/logs sans
images privées ni jetons. Ne marquer « installé » qu’après installation réelle ; ne marquer
« impression vérifiée » qu’après examen du tirage papier.
