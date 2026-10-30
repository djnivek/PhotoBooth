# Offline capture, protected generation and guest gallery

## Published Windows release

[Windows 0.3.1](https://github.com/djnivek/PhotoBooth/releases/tag/v0.3.1) is built from main commit `82766b02b86ba4fcbe903ffea0ceaadfe99fe491`, following [PR14](https://github.com/djnivek/PhotoBooth/pull/14). [Main CI41139279362](https://github.com/djnivek/PhotoBooth/actions/runs/41139279362) passed the complete verification, Chromium flows and extracted executable checks. The published ZIP was downloaded again and matches the tested artifact: 81,359,112 bytes, SHA256 `a7d2f00aafafbe9043c1fa67e0ab8e46a7f8103680aa97c31b87fa4bacd1c72a`.

[Delivery receipt](delivery.json), [package source/hash](windows/package.json), [four executable cases](windows/self-test.json), [native geometry](windows/native-ui.json) and [minimum-width geometry](windows/native-minimum-ui.json). Actual Windows screenshots: [992×700](windows/desktop-client.png), [960×700](windows/native-minimum.png). No booth credential is included in the ZIP. These are Windows Server 2022 checks, not the target Windows 10 PC or physical hardware.

## Feature evidence and before/after

PR head tested in Windows CI: `5cf95a5d52ab4dd2d6985510fc1bdd3646c4e4e5`, [run 41137416834](https://github.com/djnivek/PhotoBooth/actions/runs/41137416834). All local checks, Chromium tests, extracted Windows executable self-tests and native window execution passed. GitHub tested the PR merge revision `30baf8c721ab65081e918ec6d14ed02dc00fd18c`; [package receipt](windows-package-pr.json) records that exact source. This rehearsal ZIP is not the final main-branch release. This evidence is separate from physical Canon/DS620 operation and a real OpenAI render.

| Before | After |
| --- | --- |
| ![Before desktop](before-desktop.png) | ![After desktop](after-desktop.png) |
| ![Before mobile](before-mobile.png) | ![After mobile](after-mobile.png) |

Before: source `ee56e3b76bcc0472a3e4c321c5c54f61926c237b`. After: the tested source above. Chromium viewport 1280×900 and 390×844, full-page captures. Calibration charts are intentionally synthetic; no guest photo or paid generation appears here. The repeated single-pose chart verifies rendering, while separate real signed-asset byte comparisons verify original/final preservation.

- [Browser cases](browser.json): one/four poses, original/modified, desktop/mobile; loading, missing/expired link, offline/recovery and successive QR navigation.
- [Local tests](local.json), [Windows CI](windows-ci.json), [independent visual review](../../reviews/offline-gallery/impeccable.md).
- [Offline one photo](../../../.impeccable/review/offline-original-1.png), [offline four photos](../../../.impeccable/review/offline-original-4.png). Cloud configured, HTTP deliberately failing, exact reviewed print bytes preserved, no fabricated QR and no generation request.
- [Live sharing](supabase-share.json): four originals, four local effects, sheet and archive, repeated publication, zero generation calls.
- [Live HTTP verification](live.json): deployed site bytes, all ten signed assets, individual attachment download, CORS, unauthorized API probes including gallery token as bearer.
- [SQL guardrails](guardrails.json): real transactional limits and privilege test, all changes rolled back, zero started before/after, security advisors without warnings/errors.

The first browser run exposed test-case navigation reusing an existing page; cases now navigate independently. The actual guest page also reloads its capability when a phone reuses a tab for another QR. Superseded runs were cancelled; the completed run above is the valid receipt.

The Vercel gallery is public static code only. The repository and Supabase photo bucket stay private. No plan upgrade or paid provider call. Package publication and physical rehearsal status are tracked in [HANDOFF](../../HANDOFF.md).
