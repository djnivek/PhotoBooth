# Multi-theme selection evidence

Version 0.9.0 extends the existing native Avalonia style picker without replacing its palette, illustrations or capture sequence. Before screenshots come from main `b53731e` committed fixtures. After screenshots were produced by the actual native headless UI tests, not an HTML reconstruction.

| View | Before | After |
| --- | --- | --- |
| 960 × 700 | [Six styles](before-960x700.png) | [Nine themes](after-nine-960x700.png), [new themes at the bottom](after-nine-bottom-960x700.png) |
| 1280 × 800 | [Single choice](before-1280x800.png) | [Nine themes](after-nine-1280x800.png) |
| 20-theme stress catalog | Not available | [Four choices at 960 × 700](after-20-four-960x700.png), [three choices](after-20-three-960x700.png), [1280 × 800](after-20-four-1280x800.png) |
| Result | Single-theme sheet | [Neutral mixed-theme sheet](mixed-result-960x700.png) |

Twenty themes belong to the test fixture, including a 48-character label, 140-character description and unknown artwork. Production contains nine themes plus the separate Chef action. The last theme is reached by scrolling, clicked through native hit testing and remains selected without resetting scroll. Footer actions remain visible. After four choices other choices are disabled, while selected choices can still be removed. Hover preserves the selected outline. The mixed result contains four distinct synthetic fixtures in capture order; these are not paid generated portraits.

[Local test receipt and screenshot hashes](local-validation.json). Full C# suites, native headless UI, Deno and Node passed. SQL constraints were verified with a real remote transaction and rollback. The narrow manual Impeccable detector returned no findings for the changed native surface. No new gallery design changes or unrelated hook suppressions were made.

The chosen cream/sage/burgundy world and matte artwork are retained at Kevin's explicit request. Three generated PNGs and their exact prompts are in [Assets/Party](../../../src/PhotoBooth.App/Assets/Party/). No new animation, network polling or image download was added to the picker. Twenty is the tested UI volume, not a performance claim for an unlimited catalog. Physical camera, printer, the old i7 and exact Windows 10 host remain manual checks.
