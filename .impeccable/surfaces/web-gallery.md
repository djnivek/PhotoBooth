# Guest gallery

Mode: Experience, with direct downloads. Extension of the approved Colors Party world.

Kevin wants the QR to open a website containing both the originals and transformed photos, including photos that arrive while the guest leaves the booth for the next group. Preserve the approved cream, velvet, sage, Bricolage typography and gouache wordmark. This is an implemented extension of the existing identity, not a new visual world. The approximate one-to-two-minute wait, with a longer-wait caveat, is explicitly requested for this task and overrides DESIGN.md's older no-estimate wording on this surface.

## Composition and downloads

The opening viewport presents the approved wordmark with by Estelle, a warm title, truthful processing or ready feedback, expiry and the beginning of the first pose. While processing, originals and transformed photos appear as they become available; pending positions show preparation text. Each numbered pose pairs its unedited original with its transformation, side by side on desktop and in sequence on phones. Au naturel and explicit original-fallback sessions show each photo once. A completed fallback exposes originals without replacing partial transformations. The print master follows the photographs once final publication is ready.

The signature interaction is one archive download or an individual download beside the matching photograph or 4×6 sheet. Individual downloads are available with their uploaded photos; the complete archive and sheet appear only in the ready state. Photographs retain their proportions within a 3:2 frame. The interface recedes around pictures. Processing reuses the approved chosen-universe emblem with a subtle float and indeterminate loader; there is no custom shadow or gradient.

## Browser adaptation

Cream paper, burgundy text and primary actions, sand secondary actions, and the sage-and-wine wordmark map directly from DESIGN.md. Actions keep 12px corners; photographs use 4px corners.

Locally bundled Bricolage Grotesque loads with `font-display: swap` for headings and by Estelle. Headings use weight 600 and line height 1.12; the title uses `clamp(32px, 4.8vw, 52px)`, −0.025em tracking and a 15ch measure. At 600px and below it becomes 34px/14ch. Pose headings are 28px, or 26px on phones. Segoe UI with Arial and sans-serif fallbacks serves body text and actions; the introduction is 20px/18px, action labels 17px and metadata 14px. Paragraph line height is 1.5. The native display threshold does not govern this browser hierarchy.

The shell is at most 1120px wide, with 36px horizontal padding. Photo pairs use equal columns with a 24px gap; single-photo rows and the sheet are at most 760px wide. At 600px and below, padding becomes 20px, the wordmark centers above the title, pairs stack with a 20px gap, and actions fill the width. Pose spacing is 42px on desktop and 36px on phones. Actions are at least 48px tall; individual download links are at least 44px tall. Focus uses a 3px velvet outline with a 5px offset.

The processing state has a compact phone header at 600px and below: a 100px wordmark beside a 28px title, with the introductory sentence hidden. Its status area places a 64px emblem and loader beside the wait copy, refresh action and expiry. The first actual photo and label remain visible in the 390 × 844 opening viewport. Other states retain the existing centered phone header. Desktop processing uses a 112px emblem above a 240px-wide loader. The artwork rises 5px and returns over 2.5 seconds, and the loader travels over 1.8 seconds; `prefers-reduced-motion` disables both animations and leaves a static loader.

## Loading, expiry and offline behavior

Opening and retry feedback uses a polite live status. During processing, the page shows the approximate wait with a longer-wait caveat, the actual number of uploaded transformed photos, the chosen emblem and Actualiser. It refreshes the read-only resolver every four seconds, with at most 150 automatic refreshes. Ready, failed, delayed and request-error states stop automatic polling; leaving the page clears its timer. Manual refresh starts a fresh bounded polling cycle if still processing. This polling reads uploaded photos and never restarts generation.

Ready shows photographs, archive and expiry, with Actualiser les liens. Failed or delayed preparation retains available photos and explains the next step; pending positions become unavailable messages. A failed resolution clears photographs, sheet and expiry and hides the archive. A valid-format token keeps Réessayer available; a malformed or missing token directs the guest to rescan the QR. Image-load failure invites manual link refresh.

The browser needs connectivity to resolve links and retrieve images. Online Métamorphose obtains a stable QR during processing, before results are ready. Once that QR exists, Une nouvelle séance lets the next group use the booth while the prior controller finishes and publishes its final sheet and archive. The booth app must stay open; this is not a durable upload queue. Au naturel can prepare a one- or four-photo sheet locally and obtains its QR during result publication. A sharing network failure leaves the sheet and printing available, shows Réessayer le partage and explains that sharing awaits connectivity. That native retry is hidden during an attempt and after success; the primary new-session action stays pinned below the result.

Security: authenticated `POST /sessions/{id}/gallery` creates the stable capability. The guest link keeps it in the URL fragment; the browser sends it in the POST body to `/downloads/resolve`. The resolver reads only uploaded assets and never starts, polls or retries the generation provider. No booth credential, provider key or provider response identifier reaches the browser. Strict CSP, no third-party tracking, private storage and signed asset URLs. The footer explains that the link gives access to the session and may be shared by the guest.

## Sources and evidence

Implementation: `web/gallery/index.html`, `style.css`, `gallery.js`, `vercel.json`; native sharing feedback and controller handoff in `src/PhotoBooth.App/{MainWindow,PartyScreens,GuestSharing}.cs`; progressive upload and publication in `src/PhotoBooth.Runtime/BoothCloudClient.cs`; capability and resolver in `supabase/functions/photobooth-api/cloud.ts`. The seven emblem PNGs under `web/gallery/assets` are unchanged copies of the approved native artwork.

Evidence: `docs/evidence/offline-gallery/{after,expired}-{desktop,mobile}.png`, using desktop 1280×900 and phone 390×844 viewports; native `.impeccable/review/offline-original-{1,4}.png`. The finish review in `docs/reviews/offline-gallery/impeccable.md` records disposition ship, with no material visual fix.

Progressive-sharing evidence: `artifacts/gallery/processing-{desktop,mobile}.png` and native `artifacts/progressive-0.4.0/.impeccable/review/{processing-qr-1280x800,processing-qr-960x700,shared-result-960x700}.png`. The extension review found one phone opening-viewport issue; the compact processing header was fixed and recaptured. Follow-up disposition is ship for that fix, with the first photo and label visible by about 720px and desktop intact. The review receipt is `artifacts/progressive-0.4.0/review.json`. The builder reports 18 gallery contract checks and 10 Chromium cases passed, alongside 272 C# checks, extended native UI journeys and 16 Deno backend checks, including explicit original fallback after a partial progressive result. The extracted Windows 0.4 executable passed its self-test and native UI checks. This documentation pass inspects source and sample captures without repeating those checks.

The subsequent 0.5 transport optimization keeps this interface and its review scope. Backend 0.5 and its private-input migration are deployed. A live synthetic session passed 47 checks including progressive arrival, unchanged QR, private inputs excluded and exact contents of the server-built ZIP; production Chromium passed at 1280×900 and 390×844. Receipts are in `artifacts/optimization-0.5.0/live-smoke/`. No paid generation or physical phone scan is established by these browser tests.

The static gallery is deployed at [colors-party-estelle.vercel.app](https://colors-party-estelle.vercel.app), Vercel deployment `dpl_4s5RAwe6WMUgzvZmq6iKD8D9JPLu`. Production returned HTTP 200 and the updated script and compact styles were verified. The progressive-gallery backend was deployed and passed live synthetic sharing checks plus production desktop/mobile Chromium flows. Early/partial/ready state and every downloaded file were verified without OpenAI generation.

Browser responses and images are synthetic fixtures; native captures use calibration photographs. They establish inspected layout and state presentation, not successful production photo publication, real OpenAI output or physical Canon/DS620 operation.

DESIGN.md and `.impeccable/design.json` remain unchanged. This brief owns the browser composition and adaptations; do not overwrite native tokens. Context reported pre-existing sidecar drift, which remains unmodified; the requested approximate wait is a scoped override recorded here.
