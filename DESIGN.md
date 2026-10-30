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

This system records the implemented Avalonia desktop rehearsal, targeting Windows with the same UI rehearsed on macOS. The name and direction are working choices under the delegated delivery brief; an unanswered preference is not approval. Code-led preparation for this delivery is not a confirmed standing workflow preference. No web, iOS, or Android design is asserted here.

Source authority is `src/PhotoBooth.App/App.axaml` and `src/PhotoBooth.App/MainWindow.cs`, with capture timing in `src/PhotoBooth.Core/Session.cs` and `src/PhotoBooth.Runtime/BoothController.cs`. The latest finish review accepted the corrected capture veil within the native rehearsal scope. It does not establish hardware, printing, online sharing, or Windows-device validation.

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

**Display Font:** Bricolage Grotesque, bundled as `Assets/BricolageGrotesque.ttf`; regular/upright style, variable weight. No explicit display fallback is set in the text helper.

**Body Font:** Segoe UI, with Arial fallback. Native controls inherit this family from the window.

**Character:** Broad, lively display forms give the birthday identity its voice. Familiar desktop text keeps instructions and control labels direct.

### Hierarchy

- **Display:** the home headline uses the `display` token.
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

The centered desktop window starts at 1280 × 800 and has a minimum of 960 × 700. The application frame has horizontal and vertical margins given by `window-horizontal` and `window-vertical`. A header, flexible main region, and wrapped footer form three rows; header separation is 24 and the footer’s top margin is 20.

The home surface uses proportional columns (6:5), a 38-unit separation on the choice column, and 19-unit vertical gaps in that column. Options are equal-width native buttons with a combined gap of 12. The result uses columns (7:4), a 30-unit separation, and 22/20-unit vertical grouping. The capture screen uses a flexible image stage between its title and action row, with 20-unit vertical stage margins. Photo previews scale uniformly, with maximum heights of 380 on the home surface and 430 on the result.

No breakpoint, compact spacing variant, or automatic single-column layout is implemented. Text wrapping and proportional columns provide the current resizing behavior. The surface brief’s proposed reduced spacing at 1024 × 768 is not a current code token or established responsive rule.

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

Algorithmic calibration images come from `FixtureCamera.CreateFixture`: colored fields and simple geometry for testing framing, composition, and capture order. They are neither personal photographs nor AI-generated image assets. Rehearsal files selected by the operator remain local. UI screenshots are validation evidence, not product photography. No image-generation artwork is shipped by this interface.

### Inputs / Fields

PIN fields are native TextBoxes with a minimum height of 50 and body-size text. Password characters are masked and the entry length is capped at 12. Surface, border, selection, pointer, and focus rendering are supplied by the dark Fluent theme; the app does not define custom tokens for them. Error messages use ordinary readable text rather than an invented input-error color.

### Navigation

The persistent brand and “Réglages” button sit in the header. Home, capture, result, and error states replace the central content. There is no tab bar, browser navigation, or mobile navigation variant. Settings and unlock use owned desktop dialogs; settings content scrolls. The image picker uses the native storage-provider picker.

### Countdown and capture cue

The central badge carries a five-second countdown by default. A new number starts a small scale pulse: `1 + 0.12 × exp(-elapsedMilliseconds / 110)`, sampled every 30 ms and stopped after 550 ms. The same pulse is triggered when the badge text changes to “Clic !”. No explicit reduced-motion branch is implemented.

The framing image stays visible during positioning and countdown. Entering Capturing raises a white veil to opacity 0.22 for 170 ms; a state-transition guard prevents another flash while the same capture state continues. The badge is above the veil and the veil is not hit-testable. Between captures, the rehearsal sequence allows 1500 ms for the next pose. The actual camera stream is stopped before shutter capture, so this does not promise live video during the exposure itself.

**The Live Frame Rule.** Keep the framing image visible beneath the countdown. A capture produces one restrained veil, guarded against repeated updates in the same Capturing state.

### Session and recovery states

Positioning exposes “On est prêts !”; countdown and capture replace that action with progress and the badge. Processing changes the instruction to preparation copy. Ready displays the saved composed sheet, with explicit rehearsal print and AI wording. Cancelled returns to home once capture work has stopped. Failed presents a recovery screen and confirms that already saved photos remain local. A generic action error uses an owned dialog.

The result’s QR message is a placeholder for future sharing activation. “Tester l’impression” records a verified print-preview file and explicitly reports that no physical sheet was sent. These states must not visually imply capabilities that this rehearsal has not activated.

## Do's and Don'ts

### Do:

- **Do** preserve the native Button, TextBox, owned-dialog, file-picker, and keyboard interaction semantics.
- **Do** use the exact composed image for the result preview and keep photographic content proportionate with uniform scaling.
- **Do** keep the explicit rehearsal status visible and distinguish local calibration images from real guest photography.
- **Do** preserve the brief, single capture cue and the readable badge over the framing image.
- **Do** check the actual desktop window sizes when changing copy, spacing, or controls; there is no implemented breakpoint system.

### Don't:

- **Don't** replace the result preview with an unrelated decorative image.
- **Don't** turn the capture cue into a repeating strobe or an opaque frame that hides the photograph.
- **Don't** describe calibration geometry or rehearsal filters as personal photos or generated AI artwork.
- **Don't** treat the HTML documentation specimens as a web product or as exact reproductions of platform-owned Fluent control states.
- **Don't** infer an approved permanent workflow, phone layout, compact breakpoint, or reduced-motion variant from this delivery.
