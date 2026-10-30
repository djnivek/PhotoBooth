# Infrastructure — état au 30 octobre 2026

| Ressource | État constaté |
| --- | --- |
| GitHub | [djnivek/PhotoBooth](https://github.com/djnivek/PhotoBooth), privé |
| Organisation Supabase | [PhotoBooth](https://supabase.com/dashboard/org/scludlpexfemjustncsc) |
| Projet | [PhotoBooth](https://supabase.com/dashboard/project/frkckrvsdsiwivdfyjau), Paris eu-west-3, Nano |
| Offre | Création sans souscription payante ; affichage Free à confirmer dans le dashboard, aucun upgrade effectué |
| Postgres | Quatre migrations appliquées ; sessions/jobs/assets/budget, RLS activée, accès anon/authenticated refusé |
| Storage | Bucket booth-private privé, uploads et téléchargements signés vérifiés |
| Edge Function | photobooth-api déployée, API métier active et authentifiée |
| OpenAI | Aucun appel payé ; clé absente à la dernière vérification à 17:03 UTC |
| Galerie web | Vercel colors-party-estelle déployé ; HTTP200 sans compte, code et fichiers signés vérifiés |
| QR | API 0.3.1 : galerie dans gallery_url, ZIP dans download_url |

Le CLI Supabase fonctionne sur cette organisation ; le MCP de cette session est limité aux
anciennes organisations. L’automatisation navigateur est désactivée dans Work. Aucun autre
projet Supabase n’a été modifié.

## Secrets

Ajouter la clé dans [Edge Functions → Secrets](https://supabase.com/dashboard/project/frkckrvsdsiwivdfyjau/functions/secrets),
nom exact `OPENAI_API_KEY`. Ne pas mettre la clé dans Git, le chat, l’EXE ou les journaux.

Secrets déjà configurés : `BOOTH_ACCESS_KEY_HASH`, `OPENAI_IMAGE_MODEL`, `OPENAI_RESPONSE_MODEL`,
`GALLERY_ORIGIN`. Le code borne aléatoire et un fichier de provisionnement sont dans `.local/`
sur le Mac, mode0600, ignorés par Git. Les transférer séparément et seulement à l’opérateur.
Le mot de passe DB initial est également local ; il n’est pas requis pour utiliser l’application.

`CloudSmoke` envoie uniquement des mires synthétiques en mode Original. Son reçu versionné
est dans [evidence](evidence/2026-10-30-cloud-smoke.json). Il a téléchargé le vrai ZIP,
comparé les octets des quatre images et de la planche puis rejoué le partage sans conflit.
Le compteur SQL était exactement 0 génération. Ne pas lancer de génération dans CI.

## Déployer depuis les sources

```sh
supabase link --project-ref frkckrvsdsiwivdfyjau
supabase db push --linked --dry-run
supabase db push --linked
supabase functions deploy photobooth-api --project-ref frkckrvsdsiwivdfyjau --use-api --no-verify-jwt
```

Le bearer est validé dans la fonction ; ne jamais enlever ce contrôle. Le service role ne sort
pas du serveur. Après chaque migration, relire les policies et le caractère privé du bucket.
La migration de conservation des originaux suit la migration initiale : ne pas modifier une
migration déjà appliquée, ni effacer les objets de test par SQL direct sur storage.objects.

Pour la galerie, seul `web/gallery/` est destiné au projet Vercel `prj_5KSPB6nZ2ZwTqddXNhAqJar2WGQV`, équipe `team_jzsF4GGbmDkEYQx63pfkroVS`. Aucun secret dans ce projet statique. Domaine canonique `https://colors-party-estelle.vercel.app`, également configuré dans `GALLERY_ORIGIN`. Ne pas activer GitHub Pages ni rendre le dépôt public.
