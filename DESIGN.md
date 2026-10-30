---
name: "Colors Party by Estelle"
description: "A matte gouache and velvet celebration for a native photo booth."
colors:
  velvet-burgundy: "#742C3F"
  velvet-hover: "#8D3B4E"
  warm-white: "#FFF8EC"
  selected-sage: "#D8E0C9"
  sage-ink: "#35472C"
  sage-border: "#57654A"
  party-sage: "#E0E7D8"
  halloween-coral: "#F3D5C3"
  space-lilac: "#E7DFF0"
  fantasy-butter: "#F4E6B7"
  elves-leaf: "#DCE6D7"
  disco-rose: "#F1D5D3"
  chef-butter: "#F1E4BC"
  cream-paper: "#FBF5E8"
  burgundy-ink: "#512831"
  muted-cocoa: "#705952"
  sand-control: "#E9DFD1"
  sand-hover: "#DFD0BD"
  capture-stage: "#150F1C"
  white: "#FFFFFF"
typography:
  display:
    fontFamily: "Bricolage Grotesque"
    fontSize: "44px"
    fontWeight: 600
  display-compact:
    fontFamily: "Bricolage Grotesque"
    fontSize: "38px"
    fontWeight: 600
  headline:
    fontFamily: "Bricolage Grotesque"
    fontSize: "38px"
    fontWeight: 600
  picker-title:
    fontFamily: "Bricolage Grotesque"
    fontSize: "40px"
    fontWeight: 600
  picker-title-compact:
    fontFamily: "Bricolage Grotesque"
    fontSize: "32px"
    fontWeight: 600
  title:
    fontFamily: "Bricolage Grotesque"
    fontSize: "30px"
    fontWeight: 600
  brand:
    fontFamily: "Segoe UI, Arial"
    fontSize: "26px"
    fontWeight: 600
  countdown:
    fontFamily: "Bricolage Grotesque"
    fontSize: "152px"
    fontWeight: 700
  capture-message:
    fontFamily: "Bricolage Grotesque"
    fontSize: "40px"
    fontWeight: 700
  body:
    fontFamily: "Segoe UI, Arial"
    fontSize: "20px"
    fontWeight: 400
  label:
    fontFamily: "Segoe UI, Arial"
    fontSize: "20px"
    fontWeight: 600
  tile-label:
    fontFamily: "Segoe UI, Arial"
    fontSize: "22px"
    fontWeight: 600
  button-primary:
    fontFamily: "Segoe UI, Arial"
    fontSize: "25px"
    fontWeight: 600
rounded:
  control: "12px"
  countdown-badge: "16px"
spacing:
  tile-content: "2px"
  choice-gap: "12px"
  compact-section: "14px"
  settings-group: "18px"
  section: "20px"
  result-content: "22px"
  window-vertical: "24px"
  dialog: "30px"
  window-horizontal: "36px"
components:
  button-primary:
    backgroundColor: "{colors.velvet-burgundy}"
    textColor: "{colors.warm-white}"
    typography: "{typography.button-primary}"
    rounded: "{rounded.control}"
    padding: "12px 22px"
  button-primary-hover:
    backgroundColor: "{colors.velvet-hover}"
    textColor: "{colors.warm-white}"
  button-secondary:
    backgroundColor: "{colors.sand-control}"
    textColor: "{colors.burgundy-ink}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    padding: "12px 22px"
  button-secondary-hover:
    backgroundColor: "{colors.sand-hover}"
    textColor: "{colors.burgundy-ink}"
  choice-selected:
    backgroundColor: "{colors.selected-sage}"
    textColor: "{colors.sage-ink}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    padding: "12px 22px"
  universe-tile:
    backgroundColor: "{colors.party-sage}"
    textColor: "{colors.burgundy-ink}"
    rounded: "{rounded.control}"
    padding: "10px 14px"
  chef-choice:
    backgroundColor: "{colors.chef-butter}"
    textColor: "{colors.burgundy-ink}"
    rounded: "{rounded.control}"
    padding: "12px 22px"
  text-field:
    typography: "{typography.body}"
  countdown-badge:
    backgroundColor: "{colors.burgundy-ink}"
    textColor: "{colors.white}"
    typography: "{typography.countdown}"
    rounded: "{rounded.countdown-badge}"
    padding: "0px 26px"
  header:
    textColor: "{colors.burgundy-ink}"
    typography: "{typography.brand}"
---

# Design System: Colors Party by Estelle

## Overview

**Creative North Star: "Colors Party, gouache et velours"**

Colors Party by Estelle is a warm celebration on cream paper. Approved matte gouache emblems and the sage and wine wordmark carry the personality; broad native controls keep the guest journey clear. Butter, coral and soft botanical shades support the imagery without competing with photographs.

This is the implemented replacement visual world approved by Kevin. Preserve its generated artwork, tactile illustration and restrained retro lettering. The system describes native Avalonia desktop components for Windows, rehearsed on macOS; no browser or mobile interface is defined here. The surface mode and first-viewport composition belong in the surface brief.

Source authority: `src/PhotoBooth.App/App.axaml`, `MainWindow.cs`, `PartyScreens.cs` and `OperatorAccess.cs`; catalog colors and labels in `src/PhotoBooth.Core/styles.json` and `StyleCatalog.cs`; timing in `Session.cs`, `CornerHold.cs` and `src/PhotoBooth.Runtime/BoothController.cs`. Tokens describe the current code, not sampled illustration pixels. Current evidence and validation limits are recorded in `.impeccable/surfaces/src-photobooth-app-mainwindow-cs.md`.

**Key Characteristics:**

- Cream paper, burgundy actions, sage selection and soft universe colors.
- An approved raster wordmark and seven transparent gouache emblems.
- Bundled Bricolage Grotesque headings with familiar desktop control text.
- Large touch controls, visible framing and truthful processing states.

## Colors

The cream, sage, butter, coral and velvet family is the approved identity. Frontmatter is normative; illustration pigments are baked into the approved PNGs and are not additional UI tokens.

### Primary

- **Velvet Burgundy** (`velvet-burgundy`) is the primary action and keyboard-focus color; `velvet-hover` provides pointer feedback. Warm White (`warm-white`) keeps action text clear.

### Secondary

- **Selected Sage** (`selected-sage`), Sage Ink (`sage-ink`) and Sage Border (`sage-border`) distinguish selected count and mode choices.
- **Party Sage** (`party-sage`) is both the Colors Party tile and the safe background for an unreadable remote color.

### Tertiary

- **Halloween Coral**, **Space Lilac**, **Fantasy Butter**, **Elves Leaf** and **Disco Rose** identify the six bundled universe tiles alongside Party Sage. They are catalog values, not a guarantee that every future remote universe uses the same palette.
- **Chef Butter** (`chef-butter`) separates La Surprise du Chef from named universes. The approved artwork carries richer butter, sage, coral and burgundy accents.

### Neutral

- **Cream Paper** (`cream-paper`) is the window surface; Burgundy Ink (`burgundy-ink`) is default text and the countdown badge.
- **Muted Cocoa** (`muted-cocoa`) supports helper text. Sand Control and Sand Hover (`sand-control`, `sand-hover`) cover secondary buttons and pointer feedback.
- **Capture Stage** (`capture-stage`) frames the live image; White (`white`) supplies countdown lettering and the brief received-photo veil.

**The Readable Universe Rule.** Keep burgundy ink on catalog tiles only when the supplied background reaches 4.5:1 contrast against that ink. Otherwise use party-sage. Apply the same ink to the selected outline.

The sidecar's eight-step OKLCH ramps are synthesized documentation aids, not implemented application colors. Remote color validation uses sRGB relative luminance against the actual burgundy ink. Catalog format validation alone does not prove contrast.

## Typography

**Display Font:** Bricolage Grotesque, bundled at `Assets/BricolageGrotesque.ttf` with `Assets/OFL-BricolageGrotesque.txt`. The text helper names the bundled family explicitly; no display fallback is declared.

**Body Font:** Segoe UI, Arial fallback. The window and native buttons use this family. The illustrated Colors Party lettering is a raster wordmark, not a reusable font.

**Character:** Broad display forms pair with the approved retro wordmark. Familiar body text keeps settings, instructions and choices readable.

### Hierarchy

- **Display:** `display` on the home invitation, `display-compact` under the compact condition.
- **Headline:** `headline` for processing and results. The universe picker uses `picker-title` or its compact variant.
- **Title:** `title` for pose and result instructions and “by Estelle”. The compact home tagline becomes 26 units and consequently uses the body family.
- **Brand:** `brand` for the header's plain-text product name.
- **Countdown:** `countdown` for 3, 2, 1; `capture-message` for “Gardez la pose”.
- **Body and labels:** `body`, `label`, `tile-label` and `button-primary` are the recurring control roles. Supporting descriptions use 15–19 units; processing phrases use 25 regular; settings and unlock headings use 34 and 28 semibold; the recovery heading uses 48 semibold.

**The Display Threshold Rule.** The shared text helper uses Bricolage Grotesque at 28 logical units and above, and Segoe UI with Arial fallback below. Buttons keep the desktop body family.

Source values are requested sizes and weights, not claims about physical pixels or variable-font axis positions. No custom line height, letter spacing or variation settings are applied. Text wraps. The bundled display font's presence does not establish a browser font-loading strategy.

## Layout

Measurements are Avalonia logical units; the frontmatter uses `px` for portable notation. The centered window requests 1280 × 800 and has a 960 × 700 minimum. Startup is capped to the primary display's scaled working area less 32 horizontal and 48 vertical units, subject to that minimum. The shell has 36 horizontal and 24 vertical margins, an automatic header row, a flexible content row and a 24-unit header gap. Root maximum bounds follow actual client dimensions after opening and resizing.

Compact treatment is active at width below 1100 **or** height below 760. It changes the home and universe-picker typography, artwork sizes and spacing. It is not a mobile breakpoint or a general single-column mode. Detailed home, picker, result and dialog composition is in the surface brief. Dynamic catalogs, operator settings and result actions use native scrolling. Controls must remain inside both client and containing-panel bounds.

## Elevation & Depth

Flat cream and pastel surfaces establish hierarchy around tactile raster artwork. The live-image stage is dark; photographic content stays rectangular and uses uniform scaling. There is no app-defined shadow or gradient vocabulary. Native windows and Fluent control rendering remain platform-owned.

**The Matte Surface Rule.** Let the gouache artwork supply material character. App surfaces use flat fills and no custom shadows or gradients; platform-owned Fluent controls and window decoration retain their native behavior.

## Shapes

Native buttons and universe tiles share the control corner radius. Selected count/mode buttons have a two-unit sage border; selected universe and Chef buttons have a three-unit burgundy-ink border. Keyboard focus uses a three-unit primary-burgundy border. The countdown badge uses its larger radius. No reusable decorative card, circular clipping or pill system is defined.

## Components

### Buttons and choices

Primary controls use the burgundy family, primary type and a minimum height of 70. Secondary controls use sand, body type and a minimum height of 54. Shared padding is 22 horizontally and 12 vertically, with centered text. Pointer feedback uses each hover token; other buttons, including selected choices, use sand hover with burgundy ink. Focus remains visible; disabled controls use opacity 0.45. The shared asynchronous click handler disables the invoked button for its action and restores it afterward. No custom pressed animation is defined.

Count and mode choices are equal-width native buttons with a total 12-unit gap, not separate radio/chip components. Selected state is the sage fill and border. Illustrated universe buttons stretch their content and use 14 horizontal/10 vertical padding, a label at 22 semibold and a description at 16. A selected tile uses the ink outline; the Chef choice has its own butter fill. The same background-contrast safeguard applies to remotely supplied universes.

### Artwork and containers

The product ships eight transparent PNGs under `Assets/Party`: `wordmark`, `colors_party`, `halloween`, `space`, `fantasy`, `elves`, `disco` and `chef`. The wordmark is 1448 × 1086; each emblem is 1254 × 1254. Every raster has its exact sibling `.prompt.txt` and embedded prompt/provenance metadata; the delivery's asset scan reports 8 of 8 clean. These are approved generated assets, not guest photographs. Preserve the files and provenance when packaging or replacing an asset.

Artwork is decoded once and cached at width 800 for the wordmark, 288 for emblems; views use `Stretch.Uniform`. Unknown asset identifiers choose a deterministic local fallback from Colors Party, Disco or Chef, using the identifier hash. They do not trigger remote image loads or invent a new illustration. The wordmark and theme illustrations are the identity; the photos and QR are functional output. Calibration fixtures in UI captures are test material, not proof of generated photo quality.

### Inputs and operator dialogs

Native TextBoxes have a minimum height of 50 and body-size text. Light Fluent owns field surfaces, borders, pointer, selection and focus rendering. Permanent labels accompany masked connection and settings-code fields. The new optional settings code is exactly four characters without whitespace/control characters; an existing six-to-twelve-digit credential remains readable for compatibility. Five unsuccessful attempts lock authorization for 30 seconds. The unlock error remains visible with both actions available.

Settings use native CheckBox and ComboBox controls, a scrolling owned dialog, and a native file picker for local photos. Technical settings can identify OpenAI, cost and the camera connection honestly. The manual-focus checkbox is an operator assertion after physically checking focus; it is not evidence that WPD autofocus succeeded.

### Navigation and protected access

A plain-text brand and Réglages action form the desktop header. Central content changes among home, universes, capture, processing, result and recovery; there is no tab bar or browser/mobile navigation. Réglages is visible in windowed mode. Entering fullscreen keeps settings open, then hides the header action. After dismissal, a genuine three-second lower-left hold reopens settings, requesting the optional code when configured. See the surface brief for the 64-unit gesture region and cancellation behavior. This is application access control, not an operating-system kiosk boundary.

### Countdown and received-photo cue

Default countdown is three seconds. A fresh preview and successful preparation precede each countdown, the framing remains visible, and transfer completes before the next pose. The preview stream is released before the actual shutter request, so continuous video during exposure is not promised. Four-photo sessions allow 1500 ms for the next pose after a received capture.

A changed badge value pulses with scale `1 + 0.12 × exp(-elapsedMilliseconds / 110)`, sampled every 30 ms and stopped after 550 ms. The capture message uses the same changed-text pulse. A received photo triggers a non-interactive white veil at opacity 0.18 for 170 ms; a received-count guard prevents repeats. The saved reduced-motion setting disables the pulse and veil.

**The Live Frame Rule.** Keep the framing image visible through the countdown. Show a capture cue only after a captured photo has been received, never as proof that an unconfirmed shutter succeeded.

### Processing and recovery

Processing centers the session's chosen emblem above a real state label, rotating universe phrases and controller status. The seven bundled choices each have three phrases; remote metadata may contain one to twenty. Phrases rotate every five seconds. The emblem floats vertically by `sin(elapsedSeconds × 2π / 2.5) × 5`, sampled every 80 ms. Reduced motion fixes displacement to zero and changes the timer to 1000 ms; the phrases still rotate. The processing timer stops when leaving that screen, on deactivation or at window close, and resumes on activation while processing.

**The Honest Wait Rule.** Rotating phrases and floating artwork communicate waiting, not measured completion. Show controller status separately; never invent a percentage, completion estimate or automatic retry.

The result uses the actual composed sheet with uniform scaling, print/share feedback and a new-session action. A submitted print job is reported as submitted, not physically completed. Recovery preserves captured originals when available and offers an explicit original-photo fallback; ambiguous shutters, paid generation and print submission are not automatically retried.

## Do's and Don'ts

### Do:

- **Do** preserve the approved Colors Party wordmark and matte gouache emblems with their transparency and provenance.
- **Do** use cream paper, burgundy actions and sage selection with the established contrast fallback.
- **Do** keep guest labels concise French, without AI jargon, simulation labels or dash punctuation; technical operator settings may name OpenAI.
- **Do** retain native controls, permanent field labels, visible focus and scrolling where content requires it.
- **Do** honor the saved reduced-motion setting and show the real photo sheet before printing.
- **Do** check actual client and containing-panel bounds at each supported desktop size.

### Don't:

- **Don't** restore the discarded aubergine and lemon identity or present EstellArt as the product name.
- **Don't** substitute emoji, glyph icons, glossy 3D art or procedural drawings for the approved emblems.
- **Don't** infer progress, successful capture, completed printing or connected sharing from a decorative animation.
- **Don't** turn fixture screenshots, passing mocked tests or old Windows captures into current hardware validation.
- **Don't** infer a permanent code-led workflow preference from this delivery's explicit execution choice.
