# Viewer 0.8

Branche développée depuis la release 0.7 puis remise sur le `main` `17edd1c3b0ac9b690317a5547d10d5b97d33a3b4`, qui inclut le dashboard Mac des PR19 à PR21. Le dashboard et ses tests sont conservés.

- 456 assertions C# (91 cloud), parcours UI étendu : compteur 60/9/1 de dimensions identiques, pause pendant les réglages et le partage, départ automatique unique, désactivation et impression manuelle préservées.
- 30 tests Deno avec typecheck/lint ; 43 tests Node après intégration du nouveau main : galerie, admin, modèle du dashboard et ordonnanceur du viewer.
- Chromium synthétique à 1920×1080, 1280×720 et 390×844 : fichiers JPEG réellement décodés, annonce 15 s, photo seule 30 s, quatre poses 15 s chacune, file sans interruption, cache, reprise réseau, désactivation et absence de débordement. Les horloges de test sont accélérées ; les durées de production restent les valeurs demandées.
- [Assertions SQL](assertions.sql) exécutées dans une transaction annulée, avant application de la migration : séparation propriétaire, exclusion originaux/repli/incomplet/non libéré, tests sur option, désactivation et droits privés.
- [22 contrôles réels](live-collection.json) et [Chromium contre la production](production-browser.json) : deux séances synthétiques, résultats signés exacts, galerie invités toujours complète, réglages image inchangés. Viewer temporaire désactivé après essai ; compteur de générations resté à 1. Aucun appel de génération.
- Migration `20261031144444_room_viewer.sql` appliquée ; API 0.8 déployée ; contrôle sécurité Supabase sans alerte. Viewer ajouté au site existant, déploiement `dpl_Bk2PMLemdZCjdfE6ZGDQSWKhPS3g` READY.

[Revue visuelle](../../reviews/viewer/impeccable.md) : correction du seul élément matériel demandée puis validée. [Comparaison au système existant](../../reviews/viewer/documentation.md). Les captures sont des fixtures de géométrie, sans personnes ; les QR natifs pointent vers un faux domaine. Elles ne prouvent pas le rendu des portraits générés ou du matériel.

Captures : [annonce Full HD](../../../.impeccable/review/viewer/announcement-1920.png), [photo Full HD](../../../.impeccable/review/viewer/photo-1920.png), [annonce 720p](../../../.impeccable/review/viewer/announcement-1280.png), [photo 720p](../../../.impeccable/review/viewer/photo-1280.png), [téléphone](../../../.impeccable/review/viewer/photo-390.png), [borne 1280](../../../.impeccable/review/viewer-result-1280x800.png), [borne 960](../../../.impeccable/review/viewer-result-960x700.png).

Les liens de projection, QR et URL signées des essais restent uniquement dans les artefacts locaux ignorés. Aucun secret, accès admin ni identifiant de connexion n’est versionné.
