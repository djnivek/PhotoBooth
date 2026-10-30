# Final rehearsal hardening

Base: `65d40e207ff77fb806042968d0c3a5b8bdf35ac7`. Version being prepared: 0.2.1.
The owner authorized the external code review and asked to keep the current storage plan
until tomorrow's successful trials. No paid image generation is part of validation.

## Independently reproduced defects

| Trigger | Before | Fix and regression coverage |
| --- | --- | --- |
| Press **Voir les fichiers** from a locked fullscreen result | Explorer opened without the exit PIN | The existing unlock dialog now gates folder access. Headless UI verifies wrong PIN, cancel and correct PIN, as well as protected window close. The folder is captured from the reviewed session. |
| Start the next session while a Windows print submission is pending | The completion journal dereferenced the new/null sheet; reproduced `NullReferenceException` | Keep the session and verified sheet throughout submission. Paused fake-printer tests cover success and uncertain submission after the next session starts, with original SHA/key and no journal on the new session. |
| Cancel while a USB connection ignores cancellation, then start another session | Both connection completions could mutate the active camera/live view | Treat connection as busy, wait for it to settle, reject overlap, check cancellation after connection and close the cancelled camera. A delayed transport test verifies rejection, terminal cancellation, disposal and a fresh next session. |

Each regression failed against the previous implementation and passed after the fix.
These checks use synthetic images and injected devices, not physical Canon or DNP hardware.

## External review status

Claude CLI 2.1.283, requested and confirmed model `claude-opus-5-5`, effort `max`,
output allowance 128000, no tools/MCP/browser/session persistence. Only sanitized technical
code and evidence were sent with explicit authorization. No secrets or personal photos.

The first 61-file packet exhausted output twice without a final report and was stopped after
1286 seconds. It is not a completed review. The second packet covers 18 critical production
files, includes the fixes above and requests a bounded actionable report. Its input SHA-256 is
`ca97830d349e3b1a092456668000e276e5f966e276b8e5dddce444f0fc3540de`.
Final findings and their disposition will be added before merging this delivery.

## Remaining field gates

Windows 10 Pro 22H2, EOS4000D live view/full-resolution capture, actual DS620 4×6 paper,
phone QR and one explicitly initiated paid OpenAI edit remain manual gates. Tests on
Windows Server 2022 and a static external review cannot establish those outcomes.
