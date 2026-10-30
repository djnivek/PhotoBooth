# Claude targeted review — 30 October 2026

Supplied diff: `65d40e2..496fff1`, plus architecture and test evidence brief.
This is the completed targeted review, not a whole-repository audit or an executed hardware test.

**Verdict: Ship 0.2.1 as the rehearsal build.** I found no release blocker in the supplied diff. Whole-booth readiness is still unproven until tomorrow's Canon/DS620, real-OpenAI and phone-QR gates.

**Substantiated product defects: none.** Each fix matches its regression test. This is based on your reported results; I ran nothing.
- **OpenFiles:** The folder is captured when the result screen renders. `openFolder` runs only if `kioskLocked` is false after `ShowUnlockAsync`. The UI test covers a locked click, wrong, cancelled and correct PIN, and the target folder.
- **Print journal:** `session` and `sheet` are captured before the first await. Every journal write and state change uses them. `PausedPrinter` switches session mid-spool for both success and failure.
- **Connect overlap:** `connecting` covers camera close, connect and preview start. The post-connect `ThrowIfCancellationRequested` stops a late preview. The runtime test covers rejection, no revival, cleanup and the next guest.

**Hardware-dependent risk (make it a gate tomorrow)**

1. **No time limit after cancelling during connect**
   - **File:** `BoothController.BeginAsync` and the busy branch of `MainWindow.Render`.
   - **Trigger:** A guest presses Annuler during "Connexion à la caméra…". The Canon adapter ignores the token and never returns (unplugged, asleep, or a stalled Canon SDK).
   - **Impact:**
     - `connecting` never clears, so the screen stays on "Annulation en cours…".
     - No new session can start, and Settings returns silently.
     - Recovery is PIN close and restart; check that shutdown isn't blocked too.
     - Separately, a non-`OperationCanceledException` after cancel goes to `FailAsync`. Examples are a COM error or `ObjectDisposedException`. This may show the error screen instead of Home.
   - **Fix:**
     - Add a bounded connect timeout in the adapter; abandon that handle and never reuse it.
     - Treat any exception after the session token is cancelled as a cancellation.
   - **Test:**
     - Fake whose `ConnectAsync` never completes: expect a state the operator can recover from.
     - Fake that throws `COMException` after cancel: expect Home.
     - On hardware: press Annuler during connect, including once with the camera off or unplugged.

**Verified test gaps (non-blocking)**

2. The check "Cancelled connection is closed after it settles" also passes if `CancelAsync` disposes the camera mid-connect. That is the real Canon SDK hazard. Record `DisposedWhileConnecting` in `DelayedConnectionCamera` and assert it is false, matching the label.
3. Two UI paths have no headless coverage:
   - "Annulation en cours…" changing to Home once the connect settles.
   - A second start tap during camera close or connect. `BeginAsync` now throws there; before, sessions overlapped.

   This assumes the Button helper absorbs the exception without crashing or showing an error over the live session. Add one UI test with a delayed fake camera.
4. Close is now tested only for interception and cancel. The close dialog with the correct PIN was exercised before and no longer is. Keep one assertion for it.

**Assumptions to confirm**

5. `ReadPrintBytesAsync` now requires `session.SheetAssetKey == sheet.Key`. Confirm that `UseOriginalsAsync` and the AI fallback keep both in sync. Add a print-after-`UseOriginalsAsync` test; otherwise printing could be refused there.
6. `ShareAsync` is not in the diff but deserves the same check: capture session data before any await. `BeginAsync` clears `Sheet` and `FinalAssets` while an upload may still be running.
7. `Changed` re-renders on the UI thread, and no UI gate still uses `IsTaking` (worth a quick grep).

**Operational:** A correct PIN via OpenFiles exits kiosk mode and leaves it unlocked. Add re-locking to the runbook. Consider hiding OpenFiles while locked: guests get a PIN prompt, and wrong guesses count toward the lockout.
