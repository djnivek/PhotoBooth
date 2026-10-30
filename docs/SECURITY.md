# API access and generation budget

Only a provisioned booth holding a random 256-bit bearer can create sessions, upload photographs or start/poll image generation. The executable contains no booth secret, OpenAI key or service-role key. Configure the booth secret separately in operator settings on each Windows account; DPAPI protects the local credential. The optional four-character settings code is a different credential and never authorizes the cloud API.

The Edge Function compares the bearer hash in constant time before any database/provider call. Session ownership is checked for every private route. TLS is required, redirects are disabled by the client, and the booth bearer is never attached to signed photo downloads. Supabase service-role and OpenAI credentials remain server-only. All four tables have RLS enabled, no public grants, and the claim function is invoker-only with execute restricted to service_role. The photo bucket is private.

A desktop secret is not hardware attestation. Someone controlling the provisioned Windows account could extract it. Do not distribute provisioning files with the EXE. A copied EXE or a gallery URL alone cannot generate. CORS is browser policy, not the authorization boundary.

## Server-enforced limits

`public.booth_budget`, row `event`:

| Setting | Default | Effect |
| --- | --- | --- |
| generation_enabled | true | Stop new generations when false |
| max_images | 2400 | Hard lifetime event ceiling, enough for 600 four-pose sessions; NOT a dollar estimate |
| max_per_minute | 8 | Shared rolling one-minute start ceiling |
| max_in_flight | 4 | Shared maximum of starting/running/uncertain jobs |
| started | 0 at this delivery | Counts accepted claims, including ambiguous provider calls |

All checks, job claim and counter increment share a database transaction and budget row lock. Repeated starts for the same pose do not spend again. A rejected claim never increments the counter or reaches OpenAI. No timeout automatically frees an uncertain job or resets the budget. A published session cannot start a new pose. Existing provider jobs can still finish after the kill switch; it does not refund or cancel a prior request.

Eight starts per minute allows a four-pose session without retry storms. On 429/503, originals remain available. The app never retries a paid start automatically. A global cap is an upper bound on image count, not an OpenAI monetary budget; actual price depends on model usage. Keep an additional provider budget if desired.

## Stop, revoke, recover

Stop new generations in Supabase SQL editor:

```sql
update public.booth_budget set generation_enabled=false where id='event';
```

To revoke a compromised booth credential, generate a new random secret outside Git, replace `BOOTH_ACCESS_KEY_HASH` with its SHA-256 using Supabase secrets, and provision the new secret separately. Existing sessions retain their old owner hash; their public share capability still works, but private operations require the old owner. Decide explicitly whether to migrate specific owned sessions. Never reset `started` as part of rotation.

When an uncertain/running job occupies a slot, inspect its response ID and provider outcome first. Poll existing jobs where possible. Only an operator who has checked the outcome may mark an abandoned job terminal. Do not delete the job or the local journal to retry an ambiguous generation. Standard photographs and local printing are still available during investigation.

## Guest gallery

QR uses `https://colors-party-estelle.vercel.app/#<64 hex characters>`. A separate 256-bit capability grants read-only access to exactly one published session. It is hashed in the database and sent in a POST body to `/downloads/resolve`, never in a URL query or to OpenAI. The fragment allows guests to copy/reopen their link without transmitting it to the static hosting server. Anyone receiving that link can see its photos until expiry.

The static gallery contains no keys or private photos, has no analytics or third-party scripts, blocks framing, and requests no camera/location/microphone. CSP restricts network and photos to the dedicated Supabase project. Each photo link is signed for five minutes; Actualiser les liens obtains fresh links while the seven-day session is valid. A raw ZIP signed previously remains valid until its own expiry; revoking a gallery capability is not immediate revocation of already signed URLs. There is no automatic object purge.

Official references: [private buckets](https://supabase.com/docs/guides/storage/buckets/fundamentals), [signed downloads and revocation limits](https://supabase.com/docs/guides/storage/serving/downloads).

## Verification

`tests/generation-guardrails.sql` checks real SQL permissions and limits inside a rolled-back transaction. Run it only on the dedicated PhotoBooth project during maintenance: it temporarily locks the budget, changes fixtures in the transaction, and rolls back everything. It never calls any HTTP provider. Deno tests exercise unauthorized callers, gallery tokens submitted as credentials, quota errors, parallel duplicate starts and ambiguous responses. UI tests enable cloud, force every HTTP request to fail, capture one/four standard poses, compare print bytes with the stored master, and verify no QR or generation request appears. Chromium CI covers desktop/mobile gallery states and signed download links with synthetic images.

Physical Canon, DS620 and an explicitly initiated real OpenAI render remain separate rehearsal gates.
