# Product

<!-- impeccable:product-schema 1 -->

## Platform

Native Windows desktop, rehearsed on macOS with Avalonia, plus a guest gallery for phone and desktop browsers. No native iOS or Android application.

## Stack

C# and .NET 10, Avalonia 12.1.4, local Skia imaging, Windows WPD/EDSDK camera adapters and GDI printing. Supabase Edge Function stores photos and calls OpenAI. The guest gallery is static HTML, CSS and JavaScript with a read-only photo resolver. Self-contained Windows x64 executable delivered through the private repository.

## Users and purpose

Estelle and guests at her birthday on 31 October 2026. Kevin operates the booth. Select one or four photos, Au naturel or Métamorphose, pose in live view, then 3, 2, 1 before each capture. Review the exact 4×6 sheet and optionally print. For online Métamorphose sessions, scan the QR during processing and follow the photos on a phone while the next guest uses the booth. The gallery offers each original beside its transformation as they arrive, individual downloads, then the printable sheet and a complete archive when publication finishes. Au naturel and original-fallback sessions show each photograph once.

## Operating context

Windows 10 Pro 22H2, Canon EOS 4000D, DNP DS620, 4×6 stock. Old third-generation i7 and 8 GB RAM. Rehearsal PC has neither camera nor printer. The actual booth cannot run Codex. No paid API calls in automated tests.

## Brand commitments

Kevin explicitly names the application Colors Party by Estelle and approves the matte gouache illustrations, sage, butter, coral and velvet burgundy palette. Keep that artwork; subtle 1980s cues are optional. French guest copy has no AI jargon, simulation labels or dash punctuation. Métamorphose offers Colors Party, Halloween, Dans les étoiles, Pays imaginaire, Petits lutins and Disco velours, plus La Surprise du Chef.

## Product principles

Large touch controls. Live image stays visible through countdown. A new camera preview and successful preparation precede each countdown. Transfer must complete before the next pose. Never retry an ambiguous shutter, paid generation or print submission automatically. Printed bytes match the verified preview sheet.

## Offline and sharing

Au naturel capture, local saving, sheet composition and printing do not require a network connection. A sharing network failure or timeout leaves the result and print action available, explains that sharing is waiting for a connection, and offers an explicit retry on that result screen. The retry is hidden during sharing and after success. Une nouvelle séance remains prominent in a pinned bottom action row.

An online Métamorphose session obtains a stable gallery capability before its results are ready. The processing screen shows a loader, an approximate one-to-two-minute wait with a longer-wait caveat, and the QR as soon as that link exists. Once the QR is available, a guest can start a new session while the prior session continues processing and publishes its final sheet and archive when ready. Each guest has a separate controller; this continuation requires the booth application to remain open. It is not a durable background queue. Au naturel sharing obtains its QR during result publication. If a transformation fails after all captures are saved, the guest can explicitly keep the originals and compose their sheet locally; sharing that fallback exposes originals without overwriting any partial transformations.

The browser gallery needs a connection to resolve or refresh its links and load photos. It displays uploaded photos progressively and the returned expiry date. While processing, it refreshes every four seconds, with at most 150 automatic refreshes; ready, failed, delayed and request-error states stop that polling. Failed or delayed preparation retains available photos. Guests can refresh manually. Invalid, missing, expired or failed link resolutions clear photographs and download actions. The resolver only reads uploaded assets and never starts, polls or retries the generation provider. The session link grants access to its photos and can be shared at the guest's discretion; no booth credential or provider key belongs in the browser.

Version 0.6 implements Kevin's current choice: direct Sunburst image edits, without Luna, at medium quality and 1024×1024 output. Private generation copies remain bounded to 1536px at JPEG quality 90 without cropping or upscaling; original capture files remain unchanged. Eight generation slots are shared across sessions, with separate limits of two simultaneous uploads and two downloads. Optimized inputs take priority over archival original uploads. Server-stored results arrive in the gallery before the booth downloads them, without another JPEG encoding or upload; the complete archive is assembled on the server. The deployed server migration allows up to eight in-flight jobs and sixteen starts per minute, preserving the 2400-image event budget and existing guardrails. Eight is a shared ceiling, not a required batch size. Kevin expects sessions at least two minutes apart; four-pose sessions still issue four independent edits. Provider latency and portrait fidelity remain unmeasured by the automated suite.

## Printed keepsakes

Métamorphose uses square photographs within the existing landscape 1800×1200, 300dpi, 6×4-inch print master. One complete square or four complete squares in pose order sit beside the approved Colors Party wordmark, by Estelle, a bundled theme emblem and label, and 31 octobre 2026. The decoration occupies its own right band without covering or cropping the photos. An unknown remote theme uses the bundled La Surprise du Chef print identity. Au naturel and explicit original fallback retain their existing sheet composition. The dimensions, composition source and review bounds are recorded in [.impeccable/surfaces/metamorphosis-print.md](.impeccable/surfaces/metamorphosis-print.md).

## Dynamic universes

Six built-in choices are immediately available. Fetch public metadata once in the background on startup, keep it only in memory, and refresh explicitly in settings. Opening the picker makes no request. Do not replace choices under a guest. Backend additions, removals, order and scene prompts can change without rebuilding the app. Unknown artwork uses a stable local fallback. Freeze scene prompt and chosen universe for an existing session.

## Operator access

Settings button visible in windowed mode. Optional four-character code. Entering fullscreen keeps settings open. Once dismissed, hold the lower-left corner for three seconds; authenticate if a code is configured. Short hold, movement away or cancellation must not open settings. Windows system shortcuts are outside this application lock.

## Evidence and limits

Version 0.6 local receipts in `artifacts/direct-square-0.6.0/` report 437 passing C# checks: 231 imaging, 54 runtime, 72 cloud, 26 core, 26 print preparation and 28 fake Canon sequencing checks. The cloud suite includes two overlapping four-pose sessions whose eight generation requests must start before any result is released. Native UI journeys also pass; the builder reports 22 passing Deno backend tests. These checks use synthetic images and fake camera, HTTP and printer boundaries, with no paid generation or real hardware.

The bundled print review has disposition **ship** within its scoped composition review. All fourteen one/four-photo masters were verified at 1800×1200 and 300×300dpi; seven were visually inspected, including the required `prints/halloween-4.jpg` and `prints/space-1.jpg`. These are authored square calibration fixtures, not portrait-fidelity evidence. Physical DS620 trimming and color reproduction remain unvalidated.

The 0.6 backend and concurrency migration are deployed and verified after Kevin's explicit activation approval. PR17 is open and Windows CI run 41190537146 passed on b2a0e12. No 0.6 GitHub release is published. Forty-eight live synthetic checks verify stable early QR, progressive assets and exact downloads/ZIP; production Chromium on desktop and mobile observes the automatic transition from one result to four and the final archive at the same URL. A rollback-only SQL test accepts eight jobs and rejects the ninth, verifies rate, budget and kill-switch controls, and preserves the generation counter at 1. Security advisors report no issues. The extracted Windows 0.6 executable passed all four simulated self-test journeys and native UI checks at 1280×800 and 960×700 on the Windows 11 rehearsal PC. The static gallery is deployed at [colors-party-estelle.vercel.app](https://colors-party-estelle.vercel.app). Earlier 0.5 live synthetic checks verified the early gallery, partial photo arrival, unchanged QR, private inputs excluded, ten asset downloads with exact ZIP contents and production desktop/mobile Chromium flows without generation. Those receipts do not validate the new provider path.

Desktop and phone fixture captures, native early-QR renders and synthetic transfer measurements do not establish physical-device behavior, portrait fidelity, actual 4G speed or provider latency. No current hardware validation on EOS 4000D or DS620, and no paid OpenAI render in the automated suite. WPD autofocus completion is not proven on this camera; explicit verified manual focus is the available fallback. The official SDK is not provided. A scene prompt cannot guarantee identical generated facial pixels. Supabase plan changes remain deferred until physical rehearsal succeeds.
