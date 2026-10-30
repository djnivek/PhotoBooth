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
| Start a second session in the same window, including after cancellation | Reusing the status TextBlock raised `InvalidOperationException`: it still had the previous Grid as its visual parent | Allocate a new status control per capture screen. The post-review UI tests reproduced the crash, then passed two complete consecutive sessions and cancellation followed by a fresh session. |

Each regression failed against the previous implementation and passed after the fix.
The initial complete local suite passed (Core23, Imaging89, Runtime34, print26, Canon25, cloud37,
headless UI, Deno9, gallery5), followed by [Windows CI41112773487](https://github.com/djnivek/PhotoBooth/actions/runs/41112773487).
The extracted 0.2.1 EXE completed four synthetic paths and opened its native window at
992×700 and 960×700. Downloaded archive integrity/SHA and screenshots were checked.
The post-review additions increase runtime checks to43 and cloud checks to39, and add repeated-session
and delayed-camera UI coverage. The second-session fix was made after Claude's reviewed diff;
its verification is independent regression testing, not a new external approval.
These checks use synthetic images and injected devices, not physical Canon or DNP hardware.

## External review status

Claude CLI 2.1.283, requested and confirmed model `claude-opus-5-5`, effort `max`,
output allowance 128000, no tools/MCP/browser/session persistence. Only sanitized technical
code and evidence were sent with explicit authorization. No secrets or personal photos.

Two broad packets did not complete: the 61-file packet exhausted output twice and the
18-file packet exhausted it once. They were stopped, not counted as reviews. A final targeted
review of `65d40e2..496fff1` plus architecture/test evidence completed successfully in 507 seconds.
Its input SHA-256 is `c2e09f47f6b1be18884bfbc1d164d0146977b1f6d972071cc5b09308342b5160`.
See the [unaltered report](2026-10-30-claude-report.md) and [execution receipt](2026-10-30-claude-receipt.json).

The verdict supports shipping 0.2.1 **for rehearsal**, finds no substantiated blocker in the
supplied diff and does not claim whole-booth readiness. Claude did not run tests.

## Disposition of review notes

- **Connection cleanup timing:** the delayed-camera regression now records disposal while
  connection is still in flight and explicitly rejects it. Both successful completion and a
  late COM exception after cancellation are covered.
- **Cancellation UI and repeat start:** the native headless flow now injects a delayed camera,
  checks the start button is disabled and shutter hidden while connecting, observes the pending
  cancellation screen, waits for Home and starts a fresh next session. It also covers a COM error
  after cancellation. This verifies that existing `FailAsync` handling keeps the cancelled
  session terminal and returns Home; the report's possible error-screen outcome is not reproduced.
  This new test exposed and led to fixing the separate visual-parent crash on the second session.
- **Correct PIN from close:** restored explicit coverage of unlocking through the window-close
  path and then closing successfully, in addition to wrong/cancelled/correct PIN through OpenFiles.
- **Print after original fallback:** now verifies the exact sheet SHA and submits it through a
  fake printer after an AI failure and explicit UseOriginals; no paid API or paper involved.
- **Share ownership:** inspected `ShareAsync`: session/client are captured before its first
  await, and FinalAssets/Sheet are passed as method arguments before `PublishAsync` suspends.
  The session-ID/cancellation checks prevent an old result from updating the new QR. No new defect found.
- **UI dispatch/busy state:** Changed and Frame callbacks post onto the UI dispatcher; no
  MainWindow gate uses IsTaking. The added UI tests exercise busy transitions.
- **Operator re-lock:** the Windows runbook now states that a correct PIN leaves kiosk mode
  unlocked and requires reactivating fullscreen before handing the booth back to guests.
- **Non-returning native connection:** remains an explicit hardware gate. Cancellation cannot
  forcibly interrupt arbitrary native USB calls safely. A timeout followed by reopening the same
  device could reintroduce concurrent native calls, so no unverified handle-abandonment mechanism
  is added before the EOS4000D trial. Test cancellation with the camera off/unplugged tomorrow and
  verify PIN close/restart recovery. If a driver call hangs, stop the trial and preserve the working
  DSLRBooth setup; investigate process isolation/transport-specific timeouts before event use.

## Remaining field gates

Windows 10 Pro 22H2, EOS4000D live view/full-resolution capture, actual DS620 4×6 paper,
phone QR and one explicitly initiated paid OpenAI edit remain manual gates. Tests on
Windows Server 2022 and a static external review cannot establish those outcomes.
