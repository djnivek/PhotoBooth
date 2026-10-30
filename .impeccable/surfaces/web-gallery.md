# Guest gallery

Mode: Experience, with direct downloads. Extension of the approved Colors Party world.

Kevin wants the QR result to open a website containing both the originals and transformed photos. Preserve the approved cream, velvet, sage, Bricolage typography and gouache wordmark. This is an implemented extension of the existing identity, not a new visual world.

## Composition and downloads

The opening viewport presents the approved wordmark with by Estelle, a warm title, the complete download, expiry and the beginning of the first pose. Each numbered pose pairs its unedited original with its transformation, side by side on desktop and in sequence on phones. Au naturel and original-fallback sessions show each photo once. The print master follows the photographs.

The signature interaction is one archive download or an individual download beside the matching photograph or 4×6 sheet. Photographs retain their proportions within a 3:2 frame. The interface recedes around pictures; no animation, custom shadow or gradient is introduced.

## Browser adaptation

Cream paper, burgundy text and primary actions, sand secondary actions, and the sage-and-wine wordmark map directly from DESIGN.md. Actions keep 12px corners; photographs use 4px corners.

Locally bundled Bricolage Grotesque loads with `font-display: swap` for headings and by Estelle. Headings use weight 600 and line height 1.12; the title uses `clamp(32px, 4.8vw, 52px)`, −0.025em tracking and a 15ch measure. At 600px and below it becomes 34px/14ch. Pose headings are 28px, or 26px on phones. Segoe UI with Arial and sans-serif fallbacks serves body text and actions; the introduction is 20px/18px, action labels 17px and metadata 14px. Paragraph line height is 1.5. The native display threshold does not govern this browser hierarchy.

The shell is at most 1120px wide, with 36px horizontal padding. Photo pairs use equal columns with a 24px gap; single-photo rows and the sheet are at most 760px wide. At 600px and below, padding becomes 20px, the wordmark centers above the title, pairs stack with a 20px gap, and actions fill the width. Pose spacing is 42px on desktop and 36px on phones. Actions are at least 48px tall; individual download links are at least 44px tall. Focus uses a 3px velvet outline with a 5px offset.

## Loading, expiry and offline behavior

Opening and retry feedback uses a polite live status. Success shows photographs, archive and expiry, with Actualiser les liens. A failed resolution clears photographs, sheet and expiry and hides the archive. A valid-format token keeps Réessayer available; a malformed or missing token directs the guest to rescan the QR. Image-load failure invites manual link refresh. No automatic retry is implied.

The browser needs connectivity to resolve links and retrieve images. The booth can prepare an Au naturel one- or four-photo sheet locally. A sharing network failure leaves the sheet, printing and Réessayer le partage visible, and explains that sharing awaits connectivity. The QR appears after publication succeeds. This is manual recovery, not an offline browser gallery or background upload queue.

Security: capability in the URL fragment and POST body to the dedicated read-only resolver; no booth credential or provider key in the browser. Strict CSP, no third-party tracking, private storage and signed asset URLs. The footer explains that the link gives access to the session and may be shared by the guest.

## Sources and evidence

Implementation: `web/gallery/index.html`, `style.css`, `gallery.js`, `vercel.json`; native sharing feedback in `src/PhotoBooth.App/MainWindow.cs`.

Evidence: `docs/evidence/offline-gallery/{after,expired}-{desktop,mobile}.png`, using desktop 1280×900 and phone 390×844 viewports; native `.impeccable/review/offline-original-{1,4}.png`. The finish review in `docs/reviews/offline-gallery/impeccable.md` records disposition ship, with no material visual fix.

Browser responses and images are synthetic fixtures; native captures use calibration photographs. They establish inspected layout and state presentation, not production publication, real OpenAI output or physical Canon/DS620 operation.

DESIGN.md and `.impeccable/design.json` remain unchanged and authoritative for the native system. This brief owns the browser composition and adaptations; do not overwrite native tokens.
