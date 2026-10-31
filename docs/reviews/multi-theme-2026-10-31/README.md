# Multi-theme session review, 31 October 2026

The change lets a guest choose one to four universes before an uninterrupted four-shot session. Each selected universe appears, counts remain balanced, and repeats sit diagonally on the row-major print sheet. One-photo selection replaces the preceding choice. Chef remains exclusive and selects distinct active themes where possible. The catalog stays editable on the server; nine bundled themes keep the picker immediately usable and the layout is tested with twenty entries.

## Decisions and review

- [Product consultation](claude-product-review.md), [actual model receipt](claude-product-receipt.json) and [implemented consensus](consensus.md).
- The first broader consultation produced no verdict after hitting an output limit. Its [incomplete receipt](initial-consultation-incomplete.json) is retained rather than represented as successful.
- [Implementation review](claude-implementation-review.md) and [actual model/source receipt](claude-implementation-receipt.json): SHIP, with four prompt/catalog observations. All four are addressed in the follow-up. A final bounded Opus review checks their resolution. The reviews receive sanitized source and test reports only, without secrets/photos or execution rights.

## Validation and delivery state

- Coded: native selection, immutable local options, balanced per-pose server plan, complete frozen prompts, old scalar-session compatibility, mixed-theme print label, actual per-pose generation metrics and three new illustrated universes.
- Tested locally: 29 core checks, 308 imaging assertions, 72 runtime checks, 36 print preparation checks, 34 fake Canon checks, 102 fake HTTP checks, native UI flows at two sizes, 36 Deno tests and 43 Node tests. Admin labels also pass the 17 relevant Node tests after their update.
- Schema: first validated inside a real PostgreSQL transaction and rolled back, then persistently applied before deploying the function. RLS remains enabled with no anon/authenticated table grants; quota/counters are unchanged.
- Backend 0.9.0 deployed: 39 live checks passed, including legacy scalar prompts and stable concurrent creation. Ten synthetic rows removed; no photos, jobs or metrics created. [Backend receipts](../../evidence/multi-theme/backend-live.json). The [first Windows CI](../../evidence/multi-theme/windows-pr.json) passed, including the extracted executable. Final follow-up CI, PR merge and release are tracked in [PR24](https://github.com/djnivek/PhotoBooth/pull/24).
- Follow-up fixes: 29 core, 37 Deno and 21 gallery DOM tests passed. Chromium installation on this Mac is blocked by npm certificate-chain validation (including with system CA); TLS was not disabled. Final Windows CI runs the mixed-theme Chromium cases. Final private prompt checks passed against the deployed catalog `2026-10-31.3`; the empty synthetic session was deleted.
- [Before/after screenshots and local receipt](../../evidence/multi-theme/README.md).

## Ordered manual handoff

1. Extract the entire 0.9.0 Windows ZIP and launch the executable. Check one photo, four photos, one to four selected universes, Chef, and operator catalog refresh. Use rehearsal images on a PC without hardware.
2. Make one deliberate paid portrait trial, then four only if acceptable. Check identity, differences in costumes/scenes, QR originals and modified photos, and actual network latency. The prompt and mock tests cannot prove generation quality.
3. At the booth, close DSLR Booth/EOS Utility, confirm MF on the Canon, then verify four distinct sharp JPEGs and live-view recovery. Check one DS620 4×6 sheet and the four-frame placement. A spool acknowledgment is not proof of paper output.

The generation model/quality, rate limits, quota, Supabase plan, offline original mode, Canon command sequence and print safety guards are unchanged. The previous [technical readiness review](../technical-readiness-2026-10-31/README.md) still applies.
