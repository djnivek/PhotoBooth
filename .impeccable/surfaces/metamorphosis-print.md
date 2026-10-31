---
version: 1
slug: "metamorphosis-print"
primary_target: "src/PhotoBooth.Imaging/MetamorphosisSheet.cs"
related_targets:
  - "src/PhotoBooth.Core/PrintLayout.cs"
  - "src/PhotoBooth.Core/StyleCatalog.cs"
  - "src/PhotoBooth.Core/styles.json"
  - "src/PhotoBooth.Imaging/PhotoProcessor.cs"
  - "src/PhotoBooth.Imaging/PhotoBooth.Imaging.csproj"
  - "src/PhotoBooth.Runtime/BoothController.cs"
---

# Métamorphose print keepsake

## Mode and authority

**Mode: Experience, physical keepsake.** This ordinary extension carries the approved Colors Party world onto the guest's print. Kevin's current instruction selects direct Sunburst, without Luna, at medium quality and 1024×1024 output. The physical paper remains landscape 6×4 inches. Au naturel retains its existing composition.

The implemented source and approved artwork govern this brief. `DESIGN.md` and `.impeccable/design.json` remain unchanged; the inherited world is Colors Party, gouache et velours, from candidate 3 and seed `34a3ffae` recorded in the native surface brief. No new visual identity or concept round is established here. The fixed print composition belongs to this surface, not the global native design tokens.

## Direction contract

THESIS: A birthday keepsake is led by the guest's complete photographs, with a separate illustrated band identifying the party and its universe.

OWN-WORLD: Approved Colors Party raster wordmark and matte gouache theme emblems; warm-white ground, catalog pastels and burgundy ink. The bundled Bricolage Grotesque font supplies the attribution, theme label and date. The artwork supplies material texture; flat fills and simple white photo frames supply the structure.

STORY: Keep one transformed pose or four distinct transformed poses in capture order. Recognize Colors Party by Estelle, the session's resolved bundled universe and the date, then retain the same master that was previewed and published.

FIRST VIEW: Photographs lead on the left. The right band presents the wordmark, by Estelle, theme emblem, theme name and 31 octobre 2026 in that order. The same hierarchy holds for both photo counts.

FORM: Complete square photographs on a landscape 1800×1200 master at 300dpi. Photo-safe margins are 60 pixels, the four-photo gutters are 24 pixels, and all decoration remains outside the photo cells. No cropping, stretching or decorative overlays are applied to photographs.

## Composition and typography

Measurements here are pixels in the print raster, not Avalonia logical units. `PrintLayout.Metamorphosis` accepts exactly one or four photographs.

| Count | Photo cells | Origins |
| --- | --- | --- |
| One | One 1080×1080 square | (60, 60) |
| Four | Four 528×528 squares in a 2×2 grid | (60, 60), (612, 60), (60, 612), (612, 612) |

Each cell receives an eight-pixel white frame outside its bounds. Photo content ends at x=1140 and y=1140. The warm-white field extends to x=1188; the catalog-colored right band spans the remaining 612 pixels and the full 1200-pixel height. Photos and artwork use uniform containment, preserving every source edge. Containment can scale a 1024-square result to the 1080-square one-photo cell; it does not add captured detail.

The approved wordmark is contained within (1240, 78)–(1748, 424). Bricolage Grotesque draws centered “by Estelle” at x=1494, baseline y=478, size 39. The selected bundled gouache emblem is contained within (1256, 540)–(1732, 966). The centered theme label has baseline y=1036 and size 38, reduced only as needed to fit a 492-pixel width, down to size 28. The date has baseline y=1100 and size 29. All three text roles use the established burgundy ink. The illustrated lettering remains the original raster asset.

The imaging assembly embeds the existing Party PNGs and bundled display font. The composer loads these resources locally and emits a JPEG with 300×300dpi JFIF density. It does not request new provider artwork or a generated logo. The composer adds no shadows, gradients or clipped photo corners.

## Theme and runtime contract

Bundled print identities are Colors Party, Halloween, Dans les étoiles, Pays imaginaire, Petits lutins, Disco velours and La Surprise du Chef. Each uses the corresponding existing emblem and catalog pastel. `BoothController` passes the resolved `Session.StyleId` to the composer, so a resolved Chef selection can print its selected bundled universe.

Print lookup intentionally uses `StyleCatalog.BuiltIn`. An unknown or absent style resolves to the Chef emblem, butter band and “La Surprise du Chef” label. Remote catalog artwork paths, colors and arbitrary labels do not enter the print master. This is a print-specific fallback; the native picker has its own established unknown-artwork behavior.

Only successful Métamorphose processing uses `MetamorphosisSheet.Compose`. Au naturel and explicit original fallback continue through `PhotoProcessor.Compose`. The latter keeps the previous photo rectangles and plain sheet; the imaging suite asserts unchanged Au naturel bytes. Composition leaves source-photo bytes unchanged and preserves pose order. The saved master remains the source for preview, publication and explicit print submission.

## Evidence and verification bounds

The finish receipt is `artifacts/direct-square-0.6.0/print-review.md`, with disposition **ship** for the supplied bundled-theme print masters and composition source. It does not claim a full application review, a physical print or portrait quality.

Required complete raster masters are `artifacts/direct-square-0.6.0/prints/halloween-4.jpg` and `artifacts/direct-square-0.6.0/prints/space-1.jpg`. The reviewer independently checked all fourteen JPEGs at 1800×1200 and 300×300dpi and visually inspected seven: those two plus `chef-4.jpg`, `fantasy-1.jpg`, `colors_party-4.jpg`, `elves-4.jpg` and `disco-1.jpg`. The other seven count variants received inventory and dimension/density checks. This documentation pass also opened both required masters and inspected the composition, layout, runtime branches, catalog, embedded resources and imaging assertions.

Calibration fixtures use opposite corner marks and distinct pose dots to expose cropping, overlays and ordering errors. They are authored test images, not generated guest portraits. `Imaging.log` records 231 passing imaging assertions, including square geometry, intact edges, theme fallback and unchanged natural composition. Other current receipts report 54 runtime, 72 cloud, 26 core, 26 print preparation and 28 fake Canon checks, with native UI journeys passing. The cloud source includes the barrier requiring both four-pose sessions to start all eight requests before any result is released. The builder reports 22 passing Deno tests. This documentation pass does not rerun these suites.

The 0.6 client has eight shared generation slots, two upload slots and two download slots. Its server migration is deployed for eight in-flight jobs and sixteen starts per minute without changing the 2400-image event budget. Kevin explicitly approved this print composition and activation. The 0.6 API configuration and live synthetic gallery are verified, including desktop/mobile automatic result arrival. PR17 is open and the initial Windows CI passed; no 0.6 GitHub release is published. These checks do not evaluate a paid provider render or a physical DNP sheet.

No paid generation, real Canon capture or physical DNP print supports this review. Actual facial fidelity, provider latency, 4G performance, DS620 trimming and color reproduction remain separate verification tasks. The 60-pixel photo margin is the implemented logical safe area, not a physical-printer calibration result.

## Preserved system and unresolved drift

The established palette, raster identity, matte material and bundled typography remain the shared system. This brief records the fixed print adaptation without changing `DESIGN.md` or `.impeccable/design.json`. Reported pre-existing sidecar drift and the native surface's older wait-copy discrepancy are outside this extension and are not repaired or promoted into new system rules.
