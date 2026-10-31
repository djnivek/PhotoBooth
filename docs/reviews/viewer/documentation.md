# Viewer documentation comparison

Reviewed on 31 October 2026 after the finish review's caption correction. This is an ordinary extension of the approved Colors Party world. The existing qualitative direction and token vocabulary remain authoritative; no replacement world, token extraction or new raster asset is needed. `DESIGN.md` and `.impeccable/design.json` are preserved.

The comparison uses `PRODUCT.md`, `DESIGN.md`, `.impeccable/design.json`, the viewer surface brief, `web/gallery/viewer/`, `src/PhotoBooth.App/RoomProjection.cs`, the projection runtime/backend source and the supplied captures. The existing native home and result captures provide incumbent visual evidence; they are rehearsal fixtures, not physical booth validation.

| Aspect | Incumbent | Finished extension |
| --- | --- | --- |
| Color and ground | Cream paper, burgundy ink/actions, sage selection and a dark capture stage. | The announcement and footer use the same cream and burgundy; complete photographs sit on the established dark stage. No new color role is introduced. |
| Typography | Bundled Bricolage Grotesque for display text, Segoe UI/Arial for native controls and body text. | The announcement and countdown retain the display family; helper text and operator controls retain the body family. Browser sizing is local to the surface. |
| Material and identity | Approved raster wordmark and matte gouache emblems on flat surfaces. | The existing wordmark and Chef emblem carry the identity. UI surfaces remain flat, with no added illustration or shadow vocabulary. |
| Photographic content | Native previews and sheets use uniform scaling. | The viewer uses contain fitting. The universe/pose caption occupies only the cream footer, including its separate row at 390px, leaving the complete photograph uncovered. |
| Native departure | A prominent pinned burgundy Une nouvelle séance action. | The same action reserves countdown space without shifting its footprint. The timer pauses for owned settings/dialog windows, active printing and unsuccessful or unfinished final cloud sharing. |
| Responsive composition | Native desktop bounds at 1280×800 and 960×700. | Native controls retain those bounds. The browser adds a 96px desktop footer and a 120px footer at widths up to 600px; those values remain surface guidance rather than shared native tokens. |

The finish reviewer resolved the sole material correction and recorded **ship** in [impeccable.md](impeccable.md). This documentation pass visually inspected all eight required captures and checked their PNG dimensions:

| Surface | Evidence | Verified size |
| --- | --- | --- |
| Web announcement | [1920](../../../.impeccable/review/viewer/announcement-1920.png), [1280](../../../.impeccable/review/viewer/announcement-1280.png), [390](../../../.impeccable/review/viewer/announcement-390.png) | 1920×1080, 1280×720, 390×844 |
| Web photograph | [1920](../../../.impeccable/review/viewer/photo-1920.png), [1280](../../../.impeccable/review/viewer/photo-1280.png), [390](../../../.impeccable/review/viewer/photo-390.png) | 1920×1080, 1280×720, 390×844 |
| Native result | [1280](../../../.impeccable/review/viewer-result-1280x800.png), [960](../../../.impeccable/review/viewer-result-960x700.png) | 1280×800, 960×700 |

The synthetic geometric photographs show complete edges and footer separation. The fake native QR demonstrates its layout allocation only. These images do not establish portrait fidelity, live capability access, paid-provider behavior, physical display readability, camera operation or printer output. This pass did not rerun behavioral tests or deploy services.

The appended 0.8 product section records opt-in behavior, timer pauses, release/publication eligibility, queue timings, bounded in-memory caching, disabled/reconnect states and the separate read-only capability. The surface brief records the final caption rows and native pause behavior. Shared design files remain scoped to the incumbent native system; browser-specific composition stays in the viewer brief.

Pre-existing metadata gap: the viewer brief has no primary/related target metadata, so the Impeccable context loader did not select it automatically from `web/gallery/viewer/`. It was read directly for this pass. No schema migration or unrelated drift repair was performed.
