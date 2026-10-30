---
version: 1
slug: "src-photobooth-app-mainwindow-cs"
primary_target: "src/PhotoBooth.App/MainWindow.cs"
related_targets:
  - "src/PhotoBooth.App/PartyScreens.cs"
  - "src/PhotoBooth.App/OperatorAccess.cs"
  - "src/PhotoBooth.App/App.axaml"
---

# Colors Party by Estelle, borne d’anniversaire

## Mode and authority

**Mode: Operate.** Guests select, pose and keep their photographs. Kevin operates a native Windows desktop booth; macOS runs the local rehearsal and headless Avalonia checks. There is no browser or mobile surface in this brief.

Kevin explicitly approved Colors Party by Estelle and the matte gouache/velvet artwork with cream, sage, butter, coral and burgundy. This replaces the former aubergine Estell’Art identity. The current code and approved artwork are authoritative; `DESIGN.md` records the reusable system. This task explicitly requested code-led execution. It does not establish a standing preference for future projects. No further visual approval or decision comp is required for this documented delivery.

The source direction is `docs/reviews/colors-party/direction.md`: candidate 3, party swatch composition, seed `34a3ffae`. The approved emblems and native interaction semantics govern the build. The completed review is `docs/reviews/colors-party/impeccable.md`.

## Direction contract

THESIS: A warm birthday invitation becomes a usable photo booth. Clear choices and a large next action keep a family group moving from pose to souvenir.

OWN-WORLD: Cream paper, burgundy action and text, sage selection, soft universe colors, the approved Colors Party wordmark and matte gouache emblems. Bricolage Grotesque supports display text; native body controls remain Segoe UI with Arial fallback. Exact tokens live in DESIGN.md.

STORY: Choose one or four photos, Au naturel or Métamorphose; choose a universe when appropriate; frame the group; run 3, 2, 1 for each photo; wait with the chosen emblem and truthful status; inspect the actual composed 4×6 sheet; print or download when connected.

FIRST VIEWPORT: Header brand and Réglages, generous cream paper, authored wordmark on the left, count and mode choices on the right, one wide burgundy Démarrer action. Home uses 6:5 columns, a 38-unit gap after the brand column and centered content. The wordmark occupies 470 × 350 normally, 370 × 278 compact. Home choices use 20-unit gaps normally and 14 compact, with a 44/38-unit heading. The left “by Estelle” stays 30; the tagline changes 30 to 26. No photographic placeholder or diagnostic footer is used on home.

FORM: Six illustrated universes and a separate La Surprise du Chef, followed by large live framing. The artwork supplies the festive voice. Guest text stays concise French without AI or simulation labels or dash punctuation. Technical settings may name OpenAI and actual paid behavior.

## Native layout and states

- **Window:** requested 1280 × 800, minimum 960 × 700; actual working-area and client bounds govern layout. Compact is width < 1100 OR height < 760. Required home captures are 1280 × 800, 1024 × 768 and 960 × 700. This does not create a mobile layout.
- **Universe picker:** three columns, as many rows as needed, within a native vertical ScrollViewer. Tiles have minimum heights 170 or 148 compact, emblems 78 or 66 square, and 12-unit right/bottom margins. The pinned bottom row contains Retour, La Surprise du Chef with a 52-unit emblem and C’est parti !. Selected tiles use a three-unit ink outline. Catalogs can grow without hiding the bottom actions.
- **Capture:** an automatic title row, flexible dark stage and automatic action row. Live framing stays visible under 3, 2, 1 and Gardez la pose. On est prêts ! is enabled only after the first live frame is displayed. Fresh preview and successful camera preparation precede every countdown; transfer completes before the next pose. The stream stops before the shutter request. The next-pose delay is 1500 ms. A received-count increase, not shutter dispatch, triggers the 170 ms veil.
- **Processing:** centered panel, maximum width 760, spacing 20, 200-unit chosen emblem, 38-unit heading, 25-unit phrase and 18-unit real controller status. Phrase changes occur every five seconds. Emblem motion is ±5 units over a 2.5-second sine cycle, sampled every 80 ms. There is no fabricated percentage. Reduced motion disables displacement, countdown pulse and capture veil, while phrase updates continue at a 1000 ms timer cadence. Screen exit, deactivation and close stop processing animation.
- **Result:** 7:4 columns, a 30-unit separation, 22-unit image-column gaps and 20-unit action gaps. The actual saved sheet uses uniform scaling and a maximum height of 430. The action column scrolls. The QR is initially hidden and appears only from a real returned link. Print submission is distinct from a physical sheet leaving the printer.
- **Recovery:** an honest error with explicit return or original-photo fallback when available. Captured originals remain recoverable. No ambiguous capture, paid generation or print submission is retried automatically.
- **Settings:** owned 690 × 680 native dialog with 30-unit margins, 18-unit groups and vertical scrolling. Light Fluent fields, permanent labels and native checkboxes/combo box/file picker. Hardware, local photos, online connection, catalog refresh, reduced motion and optional code are operator controls.
- **Protected access:** Réglages remains visible in windowed mode. Entering fullscreen preserves the settings dialog; dismissing it leaves the corner gesture available. A mouse-left-button or touch hold in the bottom-left 64 × 64 region must last three seconds. Release, movement outside, lost capture or deactivation cancels it. The region sits behind guest controls and cannot steal Retour/Annuler. Busy sessions reject operator access. The optional four-character code opens a 480 × 430 dialog; five failed attempts cause a 30-second lockout. Restored legacy numeric codes remain compatible. Windows shortcuts are outside this application lock.

## Dynamic universe contract

Six bundled choices are available immediately: Colors Party, Halloween, Dans les étoiles, Pays imaginaire, Petits lutins and Disco velours. La Surprise du Chef is separate. Startup makes one background metadata request; opening the picker makes none. Metadata lives only in memory and can be refreshed explicitly in settings. A background response received while a guest is choosing waits until the next home screen. Explicit operator refresh may apply while settings owns focus. An existing session freezes its selected universe/scene prompt.

Remote colors must reach 4.5:1 against actual burgundy ink; otherwise the tile uses the approved sage fallback. Unknown artwork selects a stable local Colors Party/Disco/Chef emblem. Network failure keeps the previous catalog. Source authority: `PartyScreens.cs`, `StyleCatalog.cs`, `src/PhotoBooth.Runtime/StyleCatalogClient.cs` and the server's frozen prompt handling.

## Artwork and compatibility

Eight approved transparent PNGs and exact `.prompt.txt` files live in `src/PhotoBooth.App/Assets/Party`. Embedded prompt/provenance scan: 8 of 8 clean. Do not substitute the art or interpret test fixture photography as generated guest output. The sidecar's HTML previews are documentation translations only; current native renders remain the visual evidence.

The local application-data directory remains `EstellArt` deliberately so existing sessions, configuration and credentials continue to load. It is a compatibility path, not the current product name. Review a data migration only when an explicit migration task is authorized; do not silently rename that directory with the brand.

## Review evidence and validation bounds

Current native headless captures in `.impeccable/review/` include `desktop-1280x800.png`, `desktop-1024x768.png`, `desktop-960x700.png`, `surprise-choice.png`, `styles-960x700.png`, `halloween-choice.png`, `dynamic-styles.png`, `positioning.png`, `countdown.png`, `capture-flash.png`, `processing-halloween.png`, `result.png`, `result-1024x768.png`, `surprise-result.png`, `settings.png`, `settings-bottom.png`, `unlock.png`, `unlock-error.png` and `cancel-connection.png`. They use local fixtures on macOS. Check actual client and containing-panel bounds, not just outer-window visibility.

The full finish review inspected these 19 captures and found one material issue: contrast on an arbitrary remote tile color. The fix computes contrast against actual ink and falls back to sage; the recaptured `dynamic-styles.png` shows that case. Final disposition is **ship**, limited to this scored contrast fix; the follow-up is not a second full-surface review. See `docs/reviews/colors-party/impeccable.md` and `docs/evidence/colors-party/README.md`.

The builder reports full local Release checks passed: 26 core, 89 imaging, 50 runtime, 26 print, 28 Canon sequence, 46 cloud checks, native UI journeys, 11 Deno backend and 5 gallery checks; OpenAPI validates. These tests use fake camera/HTTP and native headless renders. This documentation pass checks source, current sample captures and document consistency; it does not repeat the completed test suite.

The backend has been deployed and checked separately: `/styles` returns the six public universes without scene prompts; authentication and selection/resume/conflict handling were verified with zero uploads and zero generation calls. The receipt is `docs/evidence/colors-party/backend.json`. The OpenAI key is still absent. These checks do not validate a connected QR journey or paid output.

Windows 0.3.0 package CI is pending at this documentation handoff. Physical EOS 4000D/DS620, old-PC performance, connected QR and paid OpenAI output remain separate, unvalidated gates. Prior Windows 0.2.1 captures do not validate the current identity. WPD autofocus completion remains unproven; the explicit fallback is a physically verified manual-focus setting. Revisit these bounds after the corresponding package run or physical rehearsal, using fresh evidence. Supabase plan changes remain deferred until physical rehearsal succeeds.

## Delivery status

Current visual system documented in DESIGN.md, its schema-2 sidecar and this Operate surface brief. No code change or Git commit belongs to this documentation pass. Preserve the hardware and package distinctions above in release claims.
