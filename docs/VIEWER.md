# Projection dans la salle

La version Windows 0.8 ajoute un diaporama de salle, optionnel et désactivé par défaut. Elle conserve le dashboard privé ajouté sur le Mac dans la PR21. Le viewer de salle est une page de projection distincte de l’administration et des QR de téléchargement des invités.

## Mise en route

1. Dans les réglages de la borne, activer d’abord la connexion en ligne habituelle.
2. Cocher **Activer le diaporama dans la salle**, puis **Enregistrer la projection**.
3. Choisir **Copier le lien du viewer**. Ouvrir ce lien dans le navigateur de l’ordinateur relié à l’écran de la salle, puis **Plein écran** (ou F11).
4. Pour les essais sur le PC sans Canon, cocher aussi **Inclure les séances avec caméra simulée**, puis enregistrer. Décocher cette option avant la fête pour masquer ces essais.

Le lien contient un code de lecture distinct, dans sa partie après `#`. Il autorise la lecture des séances projetées de cette borne. Le conserver pour l’écran de la salle. Il ne contient ni clé OpenAI, ni code borne, ni code admin. Sur un autre PC, activer la projection depuis les réglages de cette nouvelle borne et reprendre le lien qu’elle fournit.

## Parcours invité

Le résultat conserve **Imprimer**, le QR et le bouton **Une nouvelle séance**. Quand la projection est activée, ce dernier contient un compteur de 60 secondes dont l’espace est réservé : les chiffres ne changent pas les dimensions du bouton. À zéro, la borne revient à l’accueil, comme lors d’un appui manuel. Elle ne déclenche pas de capture sans nouveaux invités.

Le compteur attend la réussite du partage final, puis se met en pause pendant une impression ou lorsque des réglages/dialogues sont ouverts. Une erreur de partage conserve ainsi son bouton de relance. Sans l’option, le retour reste manuel et aucun nouvel envoi vers la projection n’est ajouté.

Le départ manuel ou automatique autorise la séance pour la salle. Si les invités quittent l’écran de préparation grâce au QR précoce, l’autorisation est enregistrée, mais la projection attend toutes les photos et la publication complète. L’application doit rester ouverte pour terminer le traitement et le partage.

## Diaporama

- Annonce de 15 secondes, avec plusieurs phrases alternées.
- Une photo : 30 secondes d’affichage.
- Quatre photos : 15 secondes par photo, dans l’ordre des poses.
- Une séance déjà projetée se termine ; les suivantes attendent dans la file.
- En attendant, des photos des séances autorisées défilent au hasard, sans répéter immédiatement la même photo lorsqu’un autre choix existe.

Au naturel montre les photos prises. Métamorphose montre uniquement les résultats finaux `photo-N.jpg`. Les `original-N.jpg`, entrées de génération, planches et archives sont exclus. Un repli Métamorphose vers les originaux est également exclu de la salle. Une séance incomplète, expirée ou qui n’a pas été libérée au départ ne peut pas être projetée.

Le lecteur conserve en mémoire au plus huit séances pour éviter de télécharger leurs photos à chaque passage. Il demande les nouveautés toutes les cinq secondes ; une réponse compacte suffit si le catalogue n’a pas changé. Les URL des photos ne sont signées qu’à leur préparation. Les séances déjà entièrement projetées sur ce navigateur sont mémorisées localement par leurs identifiants, pour éviter leur réannonce au rechargement. Le lien secret et les photos ne sont pas stockés dans ce mécanisme.

Une coupure réseau laisse défiler les photos déjà chargées et affiche la reconnexion automatique. Si la projection est désactivée sur la borne, le viewer le constate à sa prochaine vérification réussie et efface l’image et le cache. Il faut une connexion sur les deux appareils ; ce viewer n’est pas un écran HDMI piloté directement par la borne.

Les mesures de consultation du viewer ne sont pas ajoutées aux ouvertures de galerie des invités, afin de préserver les statistiques d’usage des QR.

## Validation

Voir les [preuves](evidence/viewer/README.md). Les tests n’appellent pas OpenAI et n’utilisent ni Canon ni DNP. Le viewer a aussi été vérifié contre les vrais endpoints et le site déployé, avec des JPEG synthétiques. La recette physique du photomaton reste celle du [démarrage Windows](DEMARRAGE_WINDOWS.txt).
