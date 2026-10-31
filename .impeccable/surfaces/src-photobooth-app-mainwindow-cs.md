---
version: 1
slug: "src-photobooth-app-mainwindow-cs"
primary_target: "src/PhotoBooth.App/MainWindow.cs"
related_targets:
  - "src/PhotoBooth.App/PartyScreens.cs"
  - "src/PhotoBooth.App/GuestSharing.cs"
  - "src/PhotoBooth.Runtime/BoothCloudClient.cs"
  - "src/PhotoBooth.App/OperatorAccess.cs"
  - "src/PhotoBooth.App/App.axaml"
---

# Colors Party by Estelle, borne d’anniversaire

## Mode and authority

**Mode: Operate.** Guests select, pose and keep their photographs. Kevin operates a native Windows desktop booth; macOS runs the local rehearsal and headless Avalonia checks. There is no browser or mobile surface in this brief.

Kevin explicitly approved Colors Party by Estelle and the matte gouache/velvet artwork with cream, sage, butter, coral and burgundy. This replaces the former aubergine Estell’Art identity. The current code and approved artwork are authoritative; `DESIGN.md` records the reusable system. This task explicitly requested code-led execution. It does not establish a standing preference for future projects. No further visual approval or decision comp is required for this documented delivery.

The source direction is `docs/reviews/colors-party/direction.md`: candidate 3, party swatch composition, seed `34a3ffae`. The approved emblems and native interaction semantics govern the build. The original completed review is `docs/reviews/colors-party/impeccable.md`. Progressive sharing extends that world. This task explicitly requests the approximate one-to-two-minute wait with a longer-wait caveat; that copy overrides the older no-estimate wording in DESIGN.md without claiming measured progress or guaranteed completion.

## Direction contract

THESIS: A warm birthday invitation becomes a usable photo booth. Clear choices and a large next action keep a family group moving from pose to souvenir.

OWN-WORLD: Cream paper, burgundy action and text, sage selection, soft universe colors, the approved Colors Party wordmark and matte gouache emblems. Bricolage Grotesque supports display text; native body controls remain Segoe UI with Arial fallback. Exact tokens live in DESIGN.md.

STORY: Choose one or four photos, Au naturel or Métamorphose; choose a universe when appropriate; frame the group; run 3, 2, 1 for each photo; wait with the chosen emblem, loader and truthful status. When an online Métamorphose gallery link is ready, scan the QR and start the next session while photos arrive on the phone, or stay to inspect the actual composed 4×6 sheet and print.

FIRST VIEWPORT: Header brand and Réglages, generous cream paper, authored wordmark on the left, count and mode choices on the right, one wide burgundy Démarrer action. Home uses 6:5 columns, a 38-unit gap after the brand column and centered content. The wordmark occupies 470 × 350 normally, 370 × 278 compact. Home choices use 20-unit gaps normally and 14 compact, with a 44/38-unit heading. The left “by Estelle” stays 30; the tagline changes 30 to 26. No photographic placeholder or diagnostic footer is used on home.

FORM: Nine illustrated universes and a separate La Surprise du Chef, followed by large live framing. The artwork supplies the festive voice. Guest text stays concise French without AI or simulation labels or dash punctuation. Technical settings may name OpenAI and actual paid behavior.

## Native layout and states

- **Window:** requested 1280 × 800, minimum 960 × 700; actual working-area and client bounds govern layout. Compact is width < 1100 OR height < 760. Required home captures are 1280 × 800, 1024 × 768 and 960 × 700. This does not create a mobile layout.
- **Universe picker:** three columns, as many rows as needed, within a native vertical ScrollViewer. Tiles have minimum heights 170 or 148 compact, emblems 78 or 66 square, and 12-unit right/bottom margins. The pinned bottom row contains Retour, La Surprise du Chef with a 52-unit emblem and C’est parti !. Selected tiles use a three-unit ink outline. Catalogs can grow without hiding the bottom actions.
- **Capture:** an automatic title row, flexible dark stage and automatic action row. Live framing stays visible under 3, 2, 1 and Gardez la pose. On est prêts ! is enabled only after the first live frame is displayed. Fresh preview and successful camera preparation precede every countdown; transfer completes before the next pose. The stream stops before the shutter request. The next-pose delay is 1500 ms. A received-count increase, not shutter dispatch, triggers the 170 ms veil.
- **Processing:** online sharing uses 3:2 columns. The centered status panel has maximum width 620, spacing 16, a 150-unit chosen emblem, 34-unit heading, 22-unit phrase and 18-unit real controller status. A 220-by-6 loader and 19-unit approximate wait accompany Métamorphose. The other column shows a real returned gallery QR at 220 square, an explanation and a primary Une nouvelle séance action; that action is enabled only once the link exists. The full QR and action remain visible at 960 × 700. Before the link exists, Annuler remains available. With sharing disabled, processing uses a single column. Phrase changes occur every five seconds. Emblem motion is ±5 units over a 2.5-second sine cycle, sampled every 80 ms. There is no fabricated percentage. Reduced motion disables loader animation, displacement, countdown pulse and capture veil, while phrase updates continue at a 1000 ms timer cadence. Screen exit, deactivation and close stop processing animation.
- **Result:** 7:4 columns, a 30-unit separation, 22-unit image-column gaps and 20-unit action gaps. The actual saved sheet uses uniform scaling and a maximum height of 430. Sharing feedback scrolls in the action column; Imprimer, Voir les fichiers and the wide primary Une nouvelle séance stay in a pinned bottom row. The QR appears only from a real returned link, including an existing processing link while final publication runs. Réessayer le partage appears only after an error and hides during an attempt and after success. Print submission is distinct from a physical sheet leaving the printer.
- **Recovery:** an honest error with explicit return or original-photo fallback when available. Captured originals remain recoverable. No ambiguous capture, paid generation or print submission is retried automatically.
- **Settings:** owned 690 × 680 native dialog with 30-unit margins, 18-unit groups and vertical scrolling. Light Fluent fields, permanent labels and native checkboxes/combo box/file picker. Hardware, local photos, online connection, catalog refresh, reduced motion and optional code are operator controls.
- **Protected access:** Réglages remains visible in windowed mode. Entering fullscreen preserves the settings dialog; dismissing it leaves the corner gesture available. A mouse-left-button or touch hold in the bottom-left 64 × 64 region must last three seconds. Release, movement outside, lost capture or deactivation cancels it. The region sits behind guest controls and cannot steal Retour/Annuler. Busy sessions reject operator access. The optional four-character code opens a 480 × 430 dialog; five failed attempts cause a 30-second lockout. Restored legacy numeric codes remain compatible. Windows shortcuts are outside this application lock.

## Dynamic universe contract

Nine bundled choices are available immediately: Colors Party, Halloween, Dans les étoiles, Pays imaginaire, Petits lutins, Disco velours, Drôles de bêtes, Sous cloche and À croquer. La Surprise du Chef is separate. Startup makes one background metadata request; opening the picker makes none. Metadata lives only in memory and can be refreshed explicitly in settings. A background response received while a guest is choosing waits until the next home screen. Explicit operator refresh may apply while settings owns focus. An existing session freezes its selected universe/scene prompt.

Remote colors must reach 4.5:1 against actual burgundy ink; otherwise the tile uses the approved sage fallback. Unknown artwork selects a stable local Colors Party/Disco/Chef emblem. Network failure keeps the previous catalog. Source authority: `PartyScreens.cs`, `StyleCatalog.cs`, `src/PhotoBooth.Runtime/StyleCatalogClient.cs` and the server's frozen prompt handling.

## Progressive sharing contract

Authenticated `POST /sessions/{id}/gallery` creates the stable capability before transformation results exist. Each original is uploaded before its transformation; completed transformed photos are uploaded as they arrive. The guest resolver reads those assets without contacting the generation provider. Sheet and archive appear only after final publication. Explicit original fallback publishes originals and its locally composed sheet without replacing partially uploaded transformations.

Une nouvelle séance creates a new foreground controller. A prior processing controller detaches only once its gallery link exists, continues its existing work, and publishes the final sheet and archive once ready. This requires the booth app to remain open; closing it cancels the lifetime. Starting another session never automatically restarts generation. Source authority: `GuestSharing.cs`, `MainWindow.cs`, `BoothCloudClient.cs` and `web/gallery/gallery.js`; the browser composition belongs in `web-gallery.md`.

## Artwork and compatibility

Eight approved transparent PNGs and exact `.prompt.txt` files live in `src/PhotoBooth.App/Assets/Party`. Embedded prompt/provenance scan: 8 of 8 clean. Do not substitute the art or interpret test fixture photography as generated guest output. The sidecar's HTML previews are documentation translations only; current native renders remain the visual evidence.

The local application-data directory remains `EstellArt` deliberately so existing sessions, configuration and credentials continue to load. It is a compatibility path, not the current product name. Review a data migration only when an explicit migration task is authorized; do not silently rename that directory with the brand.

## Review evidence and validation bounds

Current native headless captures in `.impeccable/review/` include `desktop-1280x800.png`, `desktop-1024x768.png`, `desktop-960x700.png`, `surprise-choice.png`, `styles-960x700.png`, `halloween-choice.png`, `dynamic-styles.png`, `positioning.png`, `countdown.png`, `capture-flash.png`, `processing-halloween.png`, `result.png`, `result-1024x768.png`, `surprise-result.png`, `settings.png`, `settings-bottom.png`, `unlock.png`, `unlock-error.png` and `cancel-connection.png`. They use local fixtures on macOS. Check actual client and containing-panel bounds, not just outer-window visibility.

The full finish review inspected these 19 captures and found one material issue: contrast on an arbitrary remote tile color. The fix computes contrast against actual ink and falls back to sage; the recaptured `dynamic-styles.png` shows that case. Final disposition is **ship**, limited to this scored contrast fix; the follow-up is not a second full-surface review. See `docs/reviews/colors-party/impeccable.md` and `docs/evidence/colors-party/README.md`.

For the progressive-sharing extension, the builder reports 272 local C# checks passed: 26 core, 89 imaging, 50 runtime, 26 print, 28 Canon sequence and 53 cloud checks, plus extended native UI journeys, 16 Deno backend checks, 18 gallery contract checks and 10 Chromium cases. The backend regression includes explicit original fallback after a partial progressive result. These tests use fake camera/HTTP and native headless renders. The extracted Windows 0.4 executable also passed its self-test and native UI checks. This documentation pass checks source, current sample captures and document consistency; it does not repeat the completed test suite.

The subsequent 0.5 transport optimization leaves the interface unchanged. The builder reports 285 C# assertions, 19 Deno tests and the extended UI journey passed. The extracted 0.5 Windows executable passed its four simulated modes and native windows at 1280×800 and 960×700 on the Windows 11 rehearsal PC. Backend 0.5 and the private-input migration are deployed, with 47 live synthetic checks and two production browser viewports. Receipts are in `artifacts/optimization-0.5.0/`. Sunburst/xhigh and 1536×1024 output remain unchanged; smaller private inputs and real four-job concurrency are tested with fixtures. Actual portrait fidelity, provider delay, 4G performance and physical camera/printer behavior remain separate checks.

Current extension evidence is `artifacts/progressive-0.4.0/.impeccable/review/processing-qr-1280x800.png`, `processing-qr-960x700.png` and `shared-result-960x700.png`, plus `artifacts/gallery/processing-{desktop,mobile}.png`. All use synthetic responses and fixture photos. The extension finish review found one mobile opening-viewport issue; its compact processing header was fixed and recaptured. Follow-up disposition is **ship**, limited to confirming that fix, with the first photo and label visible in the 390 × 844 opening viewport and desktop intact. The review receipt is `artifacts/progressive-0.4.0/review.json`. This does not supersede the validation limits below.

The earlier backend was deployed and checked separately: `/styles` returns the six public universes without scene prompts; authentication and selection/resume/conflict handling were verified with zero uploads and zero generation calls. The receipt is `docs/evidence/colors-party/backend.json`. Those checks do not validate the progressive-gallery endpoints, a connected QR journey or paid output. The updated static gallery is deployed on Vercel, with HTTP 200, its script and compact styles verified; deployment details are in `web-gallery.md`. The progressive backend is now deployed; live synthetic checks verified the early/partial/ready gallery, stable QR and exact assets, with production Chromium desktop/mobile flows and no paid generation.

Windows 0.3.0 package CI completed successfully on main d217c000d0cc6efce73c965d95e311f2d9f4209f (run 41131796018). The extracted executable exercised four fixture modes and opened a native Windows Server2022 window at 992×700 and 960×700, scale1. Real desktop pixels and geometry receipts are versioned in docs/evidence/colors-party/windows; the builder inspected the PR captures and confirmed the release captures are byte-identical. This adds an execution/rendering receipt, not a new Impeccable whole-surface verdict. Physical EOS 4000D/DS620, old-PC performance, connected QR and paid OpenAI output remain separate, unvalidated gates. Prior Windows 0.2.1 captures do not validate the current identity. WPD autofocus completion remains unproven; the explicit fallback is a physically verified manual-focus setting. Revisit these bounds after the corresponding package run or physical rehearsal, using fresh evidence. Supabase plan changes remain deferred until physical rehearsal succeeds.

## Delivery status

Readiness 0.8.1 adds one local unavailable state: with Canon selected and cloud disabled, Au naturel is selected, Métamorphose uses the existing disabled control style, and the existing detail line reads « Métamorphose demande une connexion. ». Offline standard capture remains available. Local rehearsal still offers both choices. No palette, art, typography, spacing or motion changes. Native headless captures at 1280×800 and 960×700 are in `docs/evidence/technical-readiness/`; the new UI regression fails before the guard and passes afterward. The whole design context is not refreshed by this narrow hardening task.

Current visual system remains in DESIGN.md and its schema-2 sidecar; this brief records the implemented sharing extension. Both system files are preserved. Context reported pre-existing sidecar drift, which is not repaired in this task; the explicitly requested approximate wait is recorded here as a scoped override of the older no-estimate wording. No code change or Git commit belongs to this documentation pass. Preserve the hardware, deployment and package distinctions above in release claims.


## Multiple-universe extension 0.9

One photo selects one theme. Four photos select one to four themes before positioning; Chef is mutually exclusive. Native ToggleButtons expose checked state using the existing Button visual theme and palette. Selection updates the existing controls rather than rebuilding the grid, preserving scroll and focus. At four selections unchosen tiles are disabled while chosen tiles remain removable. A pinned explanation names the allocation and an empty selection disables the start action. The catalog remains three columns in a native ScrollViewer, with pinned Retour, Chef and C’est parti !. Long labels and descriptions wrap. Nine production themes and a 20-theme fixture are checked at 1280x800 and 960x700.

The server freezes per-pose prompts and assignment. Every chosen theme appears; repeated themes are diagonal on the row-major print sheet. The camera/countdown loop is unchanged. Mixed sessions use neutral Colors Party art and « Plusieurs univers » on the print master; unknown future single themes use « Votre métamorphose », never a false Chef label. Three generated gouache assets extend the approved world, with exact prompts stored beside the PNGs. Actual paid portrait quality and physical hardware remain outside automated validation.
