I ran nothing and inspected no device; this is static reading of snapshot `0b447de` plus public docs. The Canon KR and EU SDK pages returned HTTP 403, and the DNP v1.0.5.1 PDF host refused the connection. I used instead:
- the DNP DS620 Windows driver manual v1.0.5.2_1 (2018-04-27), reached through dnpphoto.eu's redirect;
- FC.SDK at commit 0800943;
- Microsoft's DeviceCapabilities page;
- the .NET 10 supported-OS list.

## Code blocker (fix before the first real-device rehearsal)

**B1 — If stopping the preview fails or times out, the shutter still fires.**
- **Where:** `CanonWpdCamera.cs:66` sets `preview=false` in a `finally`, even when `StopPreviewAsync` throws.
  - If the 5 s stop deadline expires, `BoothController.cs:257` swallows the cancellation because the live-view token is already cancelled.
  - If the stop throws an IOException, `:258` stores it in `liveFailure`, but nothing reads that after `StopLiveViewAsync()` at `:114`.
  - `CaptureAsync` only checks `!preview && prepared` (`CanonWpdCamera.cs:81`), so `TriggerAsync` runs (`:89`).
- **Trigger:** the camera rejects the end-of-preview command, or the 5 s stop wait expires. FC.SDK's CommandGate comments say a deadline "stops the waiting", not the work. A stop that never got its turn is not run later, and the shutter command then queues behind whatever was holding the connection.
- **Impact:** breaks the "preview stopped before shutter" rule on exactly the failure paths a first 4000D test will hit.
- **Fix:** `try { await link.StopPreviewAsync(stop.Token); } catch { prepared = false; throw; } finally { preview = false; }`. Give the EDSDK adapter the same guard (`if (live) throw` in `CanonCamera.CaptureAsync`; `:73`, `:92`).
- **Regression test:** give the test camera `FailStop` and `HangStop` modes. Today its `StopPreviewAsync` can never fail (`CanonChecks.cs:86`). After preparing and stopping the preview, `CaptureAsync` must throw `InvalidOperationException` with `Triggers==0`.

## Material fixes

**M1 — Paper selection against DNP geometry (your specific question).**

Short answer: the code fails closed (it never prints at the wrong size and never clips), but it is not proven able to print on the DS620.

Confirmed in code and DNP docs:
- `FindFourBySix` (`WindowsPhotoPrinter.cs:107`) accepts only driver paper sizes within ±1.5 mm of 101.6×152.4 mm.
- DNP's Print Area table (manual §2.5, p.28) gives (6x4) and PR (4x6) as 156.1×105.0 mm: 1844×1240 at 300 dpi, 3688×2480 at 600 dpi. In the units DeviceCapabilities reports (tenths of a mm) that is 1561×1050, which is +37/+34 and outside the window.
- The later physical-size check (`:68-71`, ±0.25 in) accepts both 6.00×4.00 in and 6.146×4.134 in.
- `PrintRaster.Fit` on 1844×1240 at 300 dpi gives (0,5,1844,1229): 3:2 aspect kept, no clipping, 5-pixel white rows top and bottom. At 600 dpi it gives (0,10,3688,2459). No test covers 1844×1240.
- **[Unverified]** If the cut is centred (about 22 px per long edge, 20 px per short edge), those white rows fall in the trimmed margin. At most about 22 source pixels are lost per edge, which is inside the 60 px safe margin (`PrintLayout.cs:20`). The Métamorphose side panel runs edge to edge (`MetamorphosisSheet.cs:18`), so an off-centre cut could show a white sliver there.
- DNP says High-quality is presented to applications as 600×600 and converted to 300×600 inside the driver (p.28, note 1). The comment at `PrintRaster.cs:15` and the 300×600 test assume otherwise. This is harmless because Fit handles both.

Not measured:
- The manual documents the printed image area, not the paper sizes the driver reports to DeviceCapabilities, their order, the page offsets, or where the cut lands.
- If the driver reports the documented size, in-app printing is unavailable. If it reports the nominal 4×6, it proceeds.
- Orientation is inferred from the reported width being larger than the height (`:57`). That matches Microsoft's rule that sizes are given "as if DMORIENT_PORTRAIT". If the inference is wrong, the physical check stops the job (again fail-closed).

What to do:
- Measure first (checklist step 5).
- Only if the DS620's 4×6 entry is rejected:
  - extract a pure selector accepting a short side of 1006–1060 and a long side of 1514–1571 (tenths of a mm);
  - include the driver's paper names and sizes in the error message;
  - if the reported physical width is smaller than the height, flip orientation once before the print job is created. No job exists yet, so this is not a reprint.
- Clearing the DEVMODE form-name flag (`DM_FORMNAME`) is a defensive extra **[need unverified]**.
- Tests: a selector table with 1016×1524, 1524×1016, 1050×1561, 1561×1050 and 921×1311, plus the two Fit cases above.

The app takes whichever of (6x4) and PR (4x6) the driver lists first. DNP recommends the non-PR sizes (§2.6), while `REPRISE_WINDOWS.md:38` expects "PR4×6"; tell the operator either is fine.

**M2 — Print failures that happen before anything is sent are recorded as uncertain and block printing for good.**
- **Where:** any error before the print job is created (`WindowsPhotoPrinter.cs:15-16, 44-75`): queue missing or renamed, paper not found, settings rejected, size mismatch. All of them hit `PrintAsync`'s catch-all (`BoothController.cs:199-204`), which records `needs_operator`. After that, `:189` and `Session.cs:135` refuse every later click, and the recovery doc forbids deleting the print journal.
- **Impact:** nothing was sent, yet that guest's sheet can never be printed from the app. M1 makes this likely during the rehearsal.
- **Fix:** throw a dedicated "not submitted" exception for these cases, journal it as `not_submitted`, and reset the print state so only an explicit new click retries.
- **Regression test:** a fake printer throwing that exception allows a second explicit print (2 calls); an IOException still allows only 1.

**M3 — Errors while closing the camera escape the failure and start-session handlers.**
- **Where:** `CloseCameraAsync` (`BoothController.cs:274-275`) doesn't guard `DisposeAsync`. The camera link's 5 s session close (`FcCanonConnection.cs:90`) can throw when a USB call is stuck. Stopping event polling has no deadline at all (a possible hang is already noted in `REPRISE_WINDOWS.md:52-55`).
- **Impact:**
  - In `FailAsync` (`:279`), the guest-facing error is replaced by the close error, and the `failed` manifest and telemetry are skipped.
  - In `BeginAsync` (`:71`→`:87`), the new session never starts and the **previous** session's manifest is rewritten as `failed`.
- **Plausible trigger [Unverified]:** the camera powers off while a cancelled session left it open (`CancelAsync` doesn't close it).
- **Fix:** `try { await camera.DisposeAsync(); } catch { } finally { camera = null; }`.
- **Regression test:** a camera whose `DisposeAsync` throws → manifest says `failed`, original message kept, next session starts.

**M4 — Full-resolution photo processing runs on the UI thread.**
- **Where:** `CanonWpdCamera.cs:100` (and `CanonCamera.cs:104`) call `Normalize` in a continuation that resumes on the UI thread: the capture starts from a button click with no `ConfigureAwait(false)`.
- **Cost:** a 5184×3456 JPEG decodes to about 72 MB of pixels, and `Orient` always makes a second full copy (`PhotoProcessor.cs:29-46`). Peak is about 2×72 MB per capture, plus resampling.
- **Impact:** the UI freezes during "Gardez la pose…"; the duration on the old i7 is unmeasured. This contradicts `VALIDATION.md:76`, which claims large images are processed off the UI thread.
- **Fix:** `await Task.Run(() => PhotoProcessor.Normalize(jpeg), deadline.Token)`.
- **Regression test:** inject the normalizer and assert it runs with no synchronization context.

**M5 — Without the cloud connection, Métamorphose quietly produces the rehearsal effect.**
- **Where:** if the connection code isn't entered on this PC (or fails to load), the cloud is disabled (`MainWindow.cs:90-91`). The transform then falls back to `RehearsalTransform` (`:160-162`), and Métamorphose stays selectable (`PartyScreens.cs:75-80`).
- **Impact:** with the real Canon, guests get a purple-tinted original labelled with a random theme, with no error shown.
- **Fix:** when there is no cloud and the Canon is enabled, disable Métamorphose and say why.
- **Regression test:** an app test with Canon on and cloud off → no Métamorphose start.

**M6 — Uncertain generations hold server slots permanently, and the security doc is stale.**
- **Where:** the job-claim SQL counts `uncertain` jobs toward the in-flight cap (`…215621_generation_guardrails.sql:31`), now 8 (`…094041…sql:4`). `cloud.ts` turns 5xx/408 responses (`:341-344`), exceptions (`:360-364`) and jobs running over 180 s (`:585-589`) into `uncertain`.
- **Impact:** after eight such outcomes, every new Métamorphose start fails with `generation_busy` for the rest of the night. Au naturel is unaffected.
- **Docs:** `SECURITY.md:17-18,23` still says 4 in flight and 8 per minute (actual: 8 and 16), and `:35` gives no SQL.
- **Fix (docs only):** update the numbers; add a read-only pre-party query (`select … from booth_budget; select state,count(*) from booth_jobs group by state;`) and the exact statement for releasing a slot once the operator has checked it.

## Nonblocking observations
- No test or self-test ever runs the real print submission. `PackageSelfTest.cs:25` only converts the raster, but `VALIDATION.md:117` says "raster GDI". The WPD camera link has no tests.
- `FcCanonConnection.cs:72`: if cancelling autofocus fails, the error thrown in `finally` hides the useful autofocus message.
- With RAW+JPEG, if the JPEG arrives first, the RAW transfer stays pending until the next capture (`CanonWpdCamera.cs:88`). FC.SDK's README warns that an undrained queue makes the camera reject setting changes. Require JPEG only.
- DNP "Print Re-try" defaults to Enable (manual pp.15, 17): after a paper, ribbon or cover error, the printer reprints from its own memory. The job may already have left the Windows queue, so the "check the queue" advice (`REPRISE_WINDOWS.md:66-68`) could lead to a duplicate. Document: clear the error, wait, don't resubmit.
- The manual-focus rule holds. Without manual-focus confirmation, preparation always fails (`FcCanonConnection.cs:62-66`), and no autofocus result is ever treated as success.
- Stale-callback guards (`MainWindow.cs:164-189`) and the auto-return pauses (`RoomProjection.cs:55`) match `VIEWER.md`.
- The OS claims are honest. .NET 10's supported list names Windows 10 only as 21H2 (Enterprise/IoT), 1809 and 1607; `ARCHITECTURE.md:26-27` and `SOURCES.md:17` already say 22H2 is missing.

## Hardware gates
- **H1 — WPD on the 4000D.** FC.SDK has only been verified on the EOS 6D and 450D. Things to confirm on the device:
  - detection, opening the session, and saving to the PC;
  - preview start and stop. FC.SDK's stop switches output to the camera's LCD and ignores whether that worked, so watch the LCD at the shutter;
  - the half-press/full-press shutter sequence;
  - the JPEG arriving within 35 s.
- **H2 — Manual-focus check.** It needs the camera to report both the lens name and the focus mode. If either never arrives, FC.SDK returns "not supported" and every capture is blocked.
- **H3 — Camera power and setup:** auto power-off, AC power, card, flash.
- **H4 — DS620:** paper entries, orientation, offsets, cut position, colour, Print Re-try (see M1).
- **H5 — Windows 10 22H2 launch.**
- **H6 — First paid edit:** how its duration compares with the 110 s timeout and the Supabase function's lifetime.

## Performance on 8 GB (from code, not benchmarks)
- Captures happen one at a time, about 150 MB of temporary memory each (M4).
- Métamorphose decodes four 2560 px originals in parallel (`BoothCloudClient.cs:64`), about 4×35 MB.
- Uploads and downloads are capped at 2 each, generations at 8.
- Live view keeps one pending frame (`MainWindow.cs:174-189`), and old bitmaps are disposed.
- Printing allocates an 8.6 MB pixel buffer plus two small decodes on the UI thread.
- Background sessions and their HTTP clients are disposed after publishing; I found no unbounded growth.

## Device acceptance checklist (in order)
1. Launch the packaged EXE on the target Windows 10; confirm no `startup-error.txt` appears.
2. 4000D on AC power, auto power-off disabled, JPEG L only, lens on MF and focused at guest distance. Close DSLR Booth and EOS Utility, then tick the manual-focus confirmation.
3. One-pose Au naturel: preview appears, preparation passes, the camera's screen shows the preview stopped at the shutter, one JPEG is saved, preview resumes.
4. Two consecutive four-pose sessions. Then cancel while positioning. Then switch off or unplug the camera during preview. Expect one shutter sound per pose, clear errors, and recovery without restarting the app.
5. Read-only printer measurement in PowerShell 5.1:
   `Add-Type -AssemblyName System.Drawing; $p=New-Object System.Drawing.Printing.PrinterSettings; $p.PrinterName='<DS620 queue>'; $p.PaperSizes|select PaperName,RawKind,Width,Height; $p.DefaultPageSettings.PrintableArea`
   Values are in hundredths of an inch: nominal 4×6 shows about 400×600, the DNP size about 413×615, and the app accepts only about 394–406 × 594–606. Also note the Print Re-try setting.
6. Print one Au naturel four-pose sheet and one Métamorphose sheet. Check one copy, orientation, no white sliver on the side panel, colours, and that `print.json` says `submitted`.
7. One paid Métamorphose with one pose, then four. Check the QR gallery, then check the job table for `uncertain`.
8. Offline: Au naturel capture and print; reconnect, then "Réessayer le partage".
9. Kiosk PIN, F11 and Échap, and the 60-second viewer auto-return.

## Verdict
Not ready for guests until B1 is fixed and steps 2–6 pass on the real 4000D and DS620. If they pass, offline Au naturel is a credible baseline. Physical printing depends on step 5 and may need M1 and M2. Métamorphose depends on step 7 plus M5 and M6. If the WPD link fails on the 4000D there is no in-app fallback tonight (no legally acquired EDSDK), so keep DSLR Booth working.

Sources:
- [FC.SDK @0800943](https://github.com/SharpAstro/FC.SDK/tree/0800943d2d4aaaa8ffc5da11b5ce57bfed9614a7)
- [DNP DS620 driver manual v1.0.5.2_1](https://dnpphoto.eu/wp-content/uploads/2025/08/DP-DS620_PrinterDriverInstruction_For7810_V1.0.5.2_1_English.pdf)
- [DeviceCapabilitiesW](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-devicecapabilitiesw)
- [.NET 10 supported OS](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md)