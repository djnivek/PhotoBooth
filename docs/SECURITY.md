# API access and generation budget

Only a provisioned booth holding a random 256-bit bearer can create sessions, upload photographs or start/poll image generation. The executable contains no booth secret, OpenAI key or service-role key. Configure the booth secret separately in operator settings on each Windows account; DPAPI protects the local credential. The optional four-character settings code is a different credential and never authorizes the cloud API.

The Edge Function compares the bearer hash in constant time before any database/provider call. Session ownership is checked for every private route. TLS is required, redirects are disabled by the client, and the booth bearer is never attached to signed photo downloads. Supabase service-role and OpenAI credentials remain server-only. All private application tables have RLS enabled, no public grants, and the claim function is invoker-only with execute restricted to service_role. The photo bucket is private.

A desktop secret is not hardware attestation. Someone controlling the provisioned Windows account could extract it. Do not distribute provisioning files with the EXE. A copied EXE or a gallery URL alone cannot generate. CORS is browser policy, not the authorization boundary.

## Server-enforced limits

`public.booth_budget`, row `event`:

| Setting | Default | Effect |
| --- | --- | --- |
| generation_enabled | true | Stop new generations when false |
| max_images | 2400 | Hard lifetime event ceiling, enough for 600 four-pose sessions; NOT a dollar estimate |
| max_per_minute | 16 | Shared rolling one-minute start ceiling, set by migration 20261031094041 |
| max_in_flight | 8 | Shared maximum of starting/running/uncertain jobs, set by the same migration |
| started | Read the current row | Counts accepted claims, including ambiguous provider calls |

All checks, job claim and counter increment share a database transaction and budget row lock. Repeated starts for the same pose do not spend again. A rejected claim never increments the counter or reaches OpenAI. No timeout automatically frees an uncertain job or resets the budget. A published session cannot start a new pose. Existing provider jobs can still finish after the kill switch; it does not refund or cancel a prior request.

Sixteen starts per minute allows several four-pose sessions without retry storms. On 429/503, originals remain available. The app never retries a paid start automatically. A global cap is an upper bound on image count, not an OpenAI monetary budget; actual price depends on model usage. Keep an additional provider budget if desired.

## Stop, revoke, recover

Stop new generations in Supabase SQL editor:

```sql
update public.booth_budget set generation_enabled=false where id='event';
```

To revoke a compromised booth credential, generate a new random secret outside Git, replace `BOOTH_ACCESS_KEY_HASH` with its SHA-256 using Supabase secrets, and provision the new secret separately. Existing sessions retain their old owner hash; their public share capability still works, but private operations require the old owner. Decide explicitly whether to migrate specific owned sessions. Never reset `started` as part of rotation.

When an uncertain/running job occupies a slot, inspect its response ID and provider outcome first. Poll existing jobs where possible. Only an operator who has checked the outcome may mark an abandoned job terminal. Do not delete the job or the local journal to retry an ambiguous generation. Standard photographs and local printing are still available during investigation.

Read-only pre-party checks in the project's privileged SQL editor:

```sql
select generation_enabled, max_images, started, max_per_minute, max_in_flight
from public.booth_budget where id = 'event';
select state, count(*) from public.booth_jobs group by state order by state;
select session_id, shot_index, response_id, updated_at
from public.booth_jobs where state = 'uncertain' order by updated_at;
```

Eight uncertain jobs intentionally block new generation claims. This prevents an unknown provider outcome from being treated as free capacity. After confirming that one specific job has finished or is abandoned and no worker can still complete it, an operator may release that job's slot with the statement below. Replace the zero UUID and pose index with the inspected identifiers. It marks a terminal failure, does not regenerate the pose, and does not refund or reset `started`. Never run a bulk update or change the job back to `queued`. These recovery statements were documented, not executed as part of the readiness review.

```sql
update public.booth_jobs
set state = 'failed', updated_at = now()
where session_id = '00000000-0000-0000-0000-000000000000'::uuid
  and shot_index = 0
  and state = 'uncertain'
returning session_id, shot_index, state;
```

## Guest gallery

QR uses `https://colors-party-estelle.vercel.app/#<64 hex characters>`. A separate 256-bit capability grants read-only access to exactly one published session. It is hashed in the database and sent in a POST body to `/downloads/resolve`, never in a URL query or to OpenAI. The fragment allows guests to copy/reopen their link without transmitting it to the static hosting server. Anyone receiving that link can see its photos until expiry.

The static gallery contains no keys or private photos, has no analytics or third-party scripts, blocks framing, and requests no camera/location/microphone. CSP restricts network and photos to the dedicated Supabase project. Each photo link is signed for five minutes; Actualiser les liens obtains fresh links while the seven-day session is valid. A raw ZIP signed previously remains valid until its own expiry; revoking a gallery capability is not immediate revocation of already signed URLs. There is no automatic object purge.

Official references: [private buckets](https://supabase.com/docs/guides/storage/buckets/fundamentals), [signed downloads and revocation limits](https://supabase.com/docs/guides/storage/serving/downloads).

## Verification

`tests/generation-guardrails.sql` checks real SQL permissions and limits inside a rolled-back transaction. Run it only on the dedicated PhotoBooth project during maintenance: it temporarily locks the budget, changes fixtures in the transaction, and rolls back everything. It never calls any HTTP provider. Deno tests exercise unauthorized callers, gallery tokens submitted as credentials, quota errors, parallel duplicate starts and ambiguous responses. UI tests enable cloud, force every HTTP request to fail, capture one/four standard poses, compare print bytes with the stored master, and verify no QR or generation request appears. Chromium CI covers desktop/mobile gallery states and signed download links with synthetic images.

Physical Canon, DS620 and an explicitly initiated real OpenAI render remain separate rehearsal gates.

## Publication du dépôt (audit 0.9.1)

Aucun secret détecté dans les refs/tags Git, archives de releases extraites, discussions et journaux Actions disponibles lors de l'audit du 31 octobre 2026. Le code de connexion actif a été comparé uniquement en mémoire aux contenus, sans être affiché ni sauvegardé en clair. Les preuves limitées et compteurs sont dans `docs/evidence/kiosk-categories/publication-audit.json`. Les noms/emails des auteurs et noms d'hôtes présents dans les documents/historique restent visibles avec un dépôt public. L'URL du projet Supabase n'est pas une clé d'accès. Un audit automatique ne garantit pas l'absence de toute fuite future. La visibilité du dépôt reste une décision distincte de l'audit.

La CI exécute désormais Gitleaks épinglé et vérifié par SHA256. Ne jamais ajouter un code de borne, jeton de galerie/viewer réel, clé OpenAI, service-role, fichier .env, données de profil Windows, photos d'invités ou export d'administration aux sources, pièces jointes et journaux. Les dossiers artifacts et fichiers locaux restent ignorés. Les permissions RLS et la capacité de génération restent appliquées côté serveur même si les sources et l'EXE deviennent publics.

Pour installer sur un autre PC : télécharger le ZIP de la release, vérifier son SHA256, extraire, lancer depuis un compte dédié puis saisir le code de connexion séparé. Il est possible de transporter ce seul code via un fichier texte sur une clé USB de confiance, le copier dans les réglages puis supprimer cette copie selon sa politique habituelle. Ne pas mettre ce fichier dans le ZIP ni dans Git et ne pas copier cloud-credential : son chiffrement DPAPI dépend du compte et du PC d'origine. Le code opérateur de quatre caractères se configure séparément.
