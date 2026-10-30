---
name: "Estell’Art"
description: "A festive native photo-booth rehearsal for Estelle’s birthday."
colors:
  lemon: "#F5E764"
  lemon-hover: "#FFF39B"
  aubergine: "#241931"
  plum-surface: "#3D2D49"
  plum-hover: "#6A4A79"
  lavender-white: "#F3EAF4"
  lavender-muted: "#C7B6CD"
  stage-dark: "#150F1C"
  paper-white: "#FFFFFF"
typography:
  display:
    fontFamily: "Bricolage Grotesque"
    fontSize: "57px"
    fontWeight: 600
  display-compact:
    fontFamily: "Bricolage Grotesque"
    fontSize: "49px"
    fontWeight: 600
  headline:
    fontFamily: "Bricolage Grotesque"
    fontSize: "38px"
    fontWeight: 600
  title:
    fontFamily: "Bricolage Grotesque"
    fontSize: "30px"
    fontWeight: 600
  brand:
    fontFamily: "Bricolage Grotesque"
    fontSize: "32px"
    fontWeight: 700
  countdown:
    fontFamily: "Bricolage Grotesque"
    fontSize: "152px"
    fontWeight: 700
  body:
    fontFamily: "Segoe UI, Arial"
    fontSize: "20px"
    fontWeight: 400
  label:
    fontFamily: "Segoe UI, Arial"
    fontSize: "20px"
    fontWeight: 600
  button-primary:
    fontFamily: "Segoe UI, Arial"
    fontSize: "25px"
    fontWeight: 600
rounded:
  photo-frame: "4px"
  button: "12px"
  countdown-badge: "16px"
spacing:
  compact: "12px"
  control-group: "18px"
  home-standard: "19px"
  section: "20px"
  content: "22px"
  window-vertical: "24px"
  dialog: "30px"
  window-horizontal: "36px"
components:
  button-primary:
    backgroundColor: "{colors.lemon}"
    textColor: "{colors.aubergine}"
    typography: "{typography.button-primary}"
    rounded: "{rounded.button}"
    padding: "12px 22px"
  button-primary-hover:
    backgroundColor: "{colors.lemon-hover}"
    textColor: "{colors.aubergine}"
  button-secondary:
    backgroundColor: "{colors.plum-surface}"
    textColor: "{colors.lavender-white}"
    typography: "{typography.body}"
    rounded: "{rounded.button}"
    padding: "12px 22px"
  button-secondary-hover:
    backgroundColor: "{colors.plum-hover}"
    textColor: "{colors.paper-white}"
  choice-selected:
    backgroundColor: "{colors.lavender-white}"
    textColor: "{colors.aubergine}"
    typography: "{typography.body}"
    rounded: "{rounded.button}"
    padding: "12px 22px"
  text-field:
    typography: "{typography.body}"
  photo-frame:
    backgroundColor: "{colors.paper-white}"
    rounded: "{rounded.photo-frame}"
    padding: "12px"
  countdown-badge:
    backgroundColor: "{colors.aubergine}"
    textColor: "{colors.paper-white}"
    typography: "{typography.countdown}"
    rounded: "{rounded.countdown-badge}"
    padding: "0px 26px"
---

# Design System: Estell’Art

## Overview

**Creative North Star: "Piste de fête chromatique"**

“Piste de fête chromatique” is the working visual direction: a dark aubergine room, a clear lemon action, and generous photography. Estell’Art is personal and festive without reducing the room available for posing. Large flat areas and direct French labels give the guest a short, readable path.

This system records the implemented Avalonia desktop interface, targeting Windows with native local states rehearsed on macOS. The name and direction are working choices under the delegated delivery brief; an unanswered preference is not approval. Code-led preparation for this delivery is not a confirmed standing workflow preference. No web, iOS, or Android design is asserted here.

Source authority is `src/PhotoBooth.App/App.axaml` and `src/PhotoBooth.App/MainWindow.cs`, with capture timing in `src/PhotoBooth.Core/Session.cs` and `src/PhotoBooth.Runtime/BoothController.cs`. The latest targeted finish review accepted the permanent field labels, accurate cloud-disable confirmation, and native local dialog/result states. The result at 1024 × 768 and the visible unlock error were reviewed. Connected QR sharing, physical printing, Canon hardware operation, real AI generation, and the prepared but undeployed web gallery remain outside the validated journeys. A Windows native rehearsal subsequently exposed window overflow missed by headless checks. The final review accepted the startup/work-area, compact-home, and client/panel-bound corrections with disposition `ship`, limited to the native home screen on Windows Server 2022 at scale 1: 992 × 700 initially and 960 × 700 after resizing. Estell’Art’s name and palette are unchanged.

**Key Characteristics:**

- Aubergine surfaces, pale lavender text, and a lemon primary action.
- Expressive Bricolage Grotesque display text with familiar desktop body text.
- Large native controls, visible framing during countdown, and one brief capture cue.
- Flat composition and a literal preview of the composed photo sheet.

## Colors

A warm dark stage holds pale text and a single vivid action family. Frontmatter is the normative color inventory; calibration-image colors are test data, not interface tokens.

### Primary

- **Party Lemon** (`lemon`): primary action fills and the visible keyboard-focus border.
- **Light Lemon** (`lemon-hover`): primary-button pointer feedback.

### Neutral

- **Deep Aubergine** (`aubergine`): window background, dark text on light controls, and the countdown badge.
- **Plum Surface** (`plum-surface`): secondary buttons and unselected options.
- **Lifted Plum** (`plum-hover`): pointer feedback for secondary and selected-option buttons, with white text.
- **Lavender White** (`lavender-white`): default text and selected-option fill.
- **Muted Lavender** (`lavender-muted`): supporting explanations and the rehearsal footer.
- **Dark Stage** (`stage-dark`): the capture image’s surrounding area.
- **Paper White** (`paper-white`): the home photo frame, countdown lettering, and translucent capture veil.

**The Primary Action Rule.** Lemon identifies primary buttons and keyboard focus. Selected choices use the pale lavender fill instead.

The sidecar’s eight-step color ramps are synthesized documentation aids. The application uses the frontmatter hex values; the ramps are not an implemented theme scale.

## Typography

**Display Font:** Bricolage Grotesque, bundled as `Assets/BricolageGrotesque.ttf`; regular/upright style, variable weight. No explicit display fallback is set in the text helper. The font-hook alias `Bricolage` was corrected to the documented family `Bricolage Grotesque`; no detection rule was suppressed.

**Body Font:** Segoe UI, with Arial fallback. Native controls inherit this family from the window.

**Character:** Broad, lively display forms give the birthday identity its voice. Familiar desktop text keeps instructions and control labels direct.

### Hierarchy

- **Display:** the home headline uses `display`, switching to `display-compact` under the home-only compact condition described in Layout. The subtitle changes from 22 to 20 logical units.
- **Headline:** the result title uses `headline`; the error heading is a larger local variation (48 logical units).
- **Title:** capture and result instructions use `title`; dialog headings vary locally (28 or 34 logical units).
- **Brand:** the persistent wordmark uses `brand`.
- **Countdown:** the central countdown and “Clic !” use `countdown`.
- **Body:** `body` is the desktop baseline. Supporting text ranges from 15 to 27 logical units; the helper wraps text and does not set a custom line height or letter spacing.
- **Label:** choice-section labels use `label`; primary controls use `button-primary`.

The recorded weights are the `FontWeight` requests in the application. The bundled variable font exposes weight 200–800, optical size 12–96, and width 75–100; its metadata defaults are 800, 96, and 100. No variation settings are explicitly applied by the app. Do not treat the “Regular” style label as proof that every heading renders at weight 400. The font ships with `Assets/OFL-BricolageGrotesque.txt` under the SIL Open Font License.

**The Display Threshold Rule.** The text helper uses Bricolage Grotesque at 28 logical units and above, and Segoe UI with Arial fallback below that threshold. Buttons retain the desktop body family.

## Layout

All source measurements are Avalonia logical units. The frontmatter uses `px` for portable token notation, not a claim about physical display pixels.

The centered desktop window requests up to 1280 × 800, with a minimum of 960 × 700. When a primary display is available, startup width and height are capped to its working area divided by display scaling, less 32 horizontal and 48 vertical logical units, while retaining that minimum. The application frame has horizontal and vertical margins given by `window-horizontal` and `window-vertical`. A header, flexible main region, and wrapped footer form three rows; header separation is 24 and the footer’s top margin is 20.

On opening and resizing, the root layout’s maximum width and height are recomputed from the actual client size minus those margins. Bounds checks require `Settings`, `StartSession`, and `HomeSheet` to remain inside both the client area and their containing `Header` or `SessionBody` panel. A first native capture showed “Démarrer” clipped by its panel even though a window-only check was insufficient. The native smoke resizes from 992 × 700 to 960 × 700 at scale 1. Final review of the actual Windows pixels confirmed “Démarrer”, “Réglages”, and the photo sheet fully visible at both sizes, with parent/client bounds checks passing. This closes the home-layout validation on Windows Server 2022 only; it does not validate hardware, connected QR sharing, or real AI generation.

The home surface uses proportional columns (6:5), a 38-unit separation on the choice column, and `home-standard` vertical gaps in that column. Compact home is active when client width is strictly below 1100 OR client height is strictly below 760 logical units, reevaluated on opening and resizing. It uses the 49-unit headline instead of 57, a 20-unit subtitle instead of 22, and `compact` gaps of 12 instead of 19. Secondary and primary button minimum heights stay 54 and 70. Options remain equal-width native buttons with a combined gap of 12. The result uses columns (7:4), a 30-unit separation, and 22/20-unit vertical grouping. The capture screen uses a flexible image stage between its title and action row, with 20-unit vertical stage margins. Photo previews scale uniformly, with maximum heights of 380 on the home surface and 430 on the result.

The result action column now sits in a native ScrollViewer. The reviewed local result at 1024 × 768 retains a readable sheet, print confirmation, sharing-disabled status, and new-session action without clipping. The compact adjustment applies only to the home surface. Other surfaces retain proportional columns, text wrapping, and scrolling; no automatic single-column layout is implemented.

## Elevation & Depth

Flat fills and contrast carry hierarchy. The paper frame separates the composed image from the aubergine background; the darker capture stage contains the live view. App code defines no custom shadows or gradients. Desktop-owned windows and Fluent controls retain their framework behavior.

**The Flat Surface Rule.** Application content has no custom shadows. Tonal separation and the white photo frame supply depth; native window decoration remains platform-owned.

## Shapes

Controls have gently rounded rectangular corners using `button`. The countdown uses the larger `countdown-badge` radius; the home paper frame uses the shallow `photo-frame` radius. Photograph content remains rectangular and proportionate. There is no general pill, circle-card, or decorative clipping system.

## Components

### Buttons

Large, direct native Avalonia controls with a clear primary action.

- **Primary:** lemon fill, aubergine text, the primary type token, and a minimum height of 70.
- **Secondary:** plum fill, lavender-white text, body type, and a minimum height of 54.
- **Shared:** horizontal padding 22 and vertical padding 12; centered content and the button radius.
- **Hover:** primary buttons use light lemon; all other buttons, including selected options, use lifted plum with white text.
- **Focus:** a visible lemon border (3 units).
- **Disabled/busy:** opacity 0.45. The shared click handler disables the invoked button during its asynchronous action and re-enables it afterward.
- **Pressed:** no application-specific pressed token is defined; Fluent behavior remains authoritative.

### Choice controls

The two option rows use native Buttons, not a separate chip or radio-control implementation. Selection is indicated by pale fill and aubergine text. Each row has two equal columns and reconstructs the home content when the selection changes.

### Cards / Containers

The home image has a white paper surround with the photo-frame radius and compact padding. The capture stage is a flat dark rectangle with uniform image scaling. There is no reusable decorative card collection.

Algorithmic calibration images come from `FixtureCamera.CreateFixture`: colored fields and simple geometry for testing framing, composition, and capture order. They are neither personal photographs nor AI-generated image assets. The local rehearsal can also use operator-selected photos. Online activation changes the session’s sharing and transformation path; local-only behavior must not be implied when cloud mode is enabled. UI screenshots are validation evidence, not product photography. No image-generation artwork is shipped by this interface.

### Inputs / Fields

Canon setup uses native checkboxes: “Utiliser le Canon USB (Windows)” and “Connexion avancée : SDK Canon officiel installé”. The default route uses the existing Windows driver; the visible helper says “Par défaut, connexion par le pilote Windows existant, sans SDK à installer. Compatibilité à vérifier sur le boîtier.” The advanced checkbox selects the installed official SDK route. This documents the settings and their explicit compatibility limit, not successful camera operation.

Code fields are native TextBoxes with a minimum height of 50 and body-size text. Permanent labels identify “Code de connexion de la borne · pas la clé OpenAI” and “Code de sortie du plein écran · 6 à 12 chiffres”; the unlock dialog uses “Code de sortie”. Redundant placeholders have been removed. All three fields mask their content; only exit PIN fields have the 12-character cap. Surface, border, selection, pointer, and focus rendering remain dark Fluent defaults. The visible unlock error reads “Code incorrect ou verrouillage de 30 secondes.” and uses ordinary text, with both dialog actions remaining available.

### Navigation

The persistent brand and “Réglages” button sit in the header. Home, capture, result, and error states replace the central content. There is no tab bar, browser navigation, or mobile navigation variant. Settings and unlock use owned desktop dialogs: settings is 690 × 680 with scrolling content; unlock is 480 × 430, leaving room for its permanent label, visible error, and both actions. The image picker uses the native storage-provider picker.

### Countdown and capture cue

The central badge carries a five-second countdown by default. A new number starts a small scale pulse: `1 + 0.12 × exp(-elapsedMilliseconds / 110)`, sampled every 30 ms and stopped after 550 ms. The same pulse is triggered when the badge text changes to “Clic !”. No explicit reduced-motion branch is implemented.

The framing image stays visible during positioning and countdown. Entering Capturing raises a white veil to opacity 0.22 for 170 ms; a state-transition guard prevents another flash while the same capture state continues. The badge is above the veil and the veil is not hit-testable. Between captures, the rehearsal sequence allows 1500 ms for the next pose. The actual camera stream is stopped before shutter capture, so this does not promise live video during the exposure itself.

**The Live Frame Rule.** Keep the framing image visible beneath the countdown. A capture produces one restrained veil, guarded against repeated updates in the same Capturing state.

### Session and recovery states

Positioning exposes “On est prêts !”, initially disabled until the first live-view frame has been displayed. The existing disabled opacity applies while waiting. Countdown and capture replace that action with progress and the badge. Processing changes the instruction to preparation copy. Ready displays the saved composed sheet, with wording based on actual print/cloud settings and effective photo mode. Cancelled returns to home once capture work has stopped. Failed preserves saved photos and, when all original captures exist after a Surprise failure, offers “Garder les originaux”. A generic action error uses an owned dialog.

“Tester l’impression” writes a print-preview file and reports that no sheet was sent. With physical printing enabled, the action becomes “Imprimer” and reports the submitted spool job; that status is not proof of a physical print. The footer independently identifies camera, printing, and cloud modes.

Cloud activation and deactivation are explicit operator actions. Activation checks the connection and distinguishes OpenAI readiness from sharing-only readiness. Deactivation reports exactly “Connexion en ligne désactivée. Aucun appel OpenAI payant.”; it does not announce that the camera or physical printer were disabled.

### Conditional QR sharing

The result displays “Partage en ligne désactivé dans les réglages.” while cloud mode is off. When enabled, publication shows preparation/upload status and a retry action. The QR becomes visible only after successful publication for the still-current session; errors remain in the status text. It is generated at exactly 3 PNG pixels per module and displayed at `ModuleMatrix.Count × 3` logical units per side, avoiding arbitrary rescaling in the source layout. The text supplies the expiry date and identifies ZIP download when no gallery URL is configured.

No connected QR is present in the reviewed local captures, so its appearance and scan flow are implementation facts pending visual/device validation. The web gallery is prepared but undeployed; it is not an established, visually validated extension of this native design system.

## Do's and Don'ts

### Do:

- **Do** preserve native controls, owned dialogs, scrolling, file-picker, and keyboard interaction semantics, with permanent labels for code fields.
- **Do** use the exact composed image for the result preview and keep photographic content proportionate with uniform scaling.
- **Do** keep camera, printing, and cloud status explicit and distinguish local calibration images from real guest photography.
- **Do** preserve the brief, single capture cue and the readable badge over the framing image.
- **Do** check actual desktop sizes and containing-panel bounds when changing copy, spacing, or controls, including both sides of the home compact thresholds and 960 × 700.

### Don't:

- **Don't** replace the result preview with an unrelated decorative image.
- **Don't** turn the capture cue into a repeating strobe or an opaque frame that hides the photograph.
- **Don't** describe calibration geometry or rehearsal filters as personal photos or generated AI artwork.
- **Don't** treat the HTML documentation specimens as a web product or as exact reproductions of platform-owned Fluent control states.
- **Don't** infer a validated connected QR or web-gallery journey, an approved permanent workflow, a phone layout, compact rules beyond the documented home condition, or a reduced-motion variant from this delivery.
