# Windows 0.2.0 delivery evidence

[Private release](https://github.com/djnivek/PhotoBooth/releases/tag/v0.2.0) ·
[Windows CI](https://github.com/djnivek/PhotoBooth/actions/runs/41105339893) ·
[Package PR](https://github.com/djnivek/PhotoBooth/pull/7).

Source commit: `c4ba8336b222237a6730fc87cd3c34a0b8273235`.
Archive: `EstellArt-0.2.0-win-x64.zip`, 71,147,879 bytes.
SHA-256: `cdd86b7c3f429fc4b329a512d3c1ffae2ffffa88e887420ead33a92653db7d57`.

The Windows runner built the self-contained executable, created the ZIP, extracted it into
a new directory and executed that extracted copy. Its self-test completed all four combinations
of one/four poses and original/simulated effect. It checked image composition, print raster,
print-intent persistence, ZIP contents, QR generation, PIN and DPAPI credential round-trip.
No physical device or OpenAI endpoint was used. The downloaded archive's SHA was verified.

The actual native window was then opened with an isolated profile. Its desktop client pixels
and rendered frame were captured at 992×700. It was resized to 960×700 and captured again.
Assertions verify that the settings button, start button and sheet stay inside the client and
their containing panels. Visual review accepted the corrected home screen at display scale 1.

| Receipt | Scope |
| --- | --- |
| [delivery.json](delivery.json) | Release, CI, source, archive inventory and remaining field gates |
| [package.json](package.json) | Packaging and executable receipt emitted by Windows |
| [self-test.json](self-test.json) | Four executed rehearsal modes and archive counts |
| [native-ui.json](native-ui.json) | Initial window geometry and screenshot SHA |
| [native-minimum-ui.json](native-minimum-ui.json) | Geometry after actual resize |
| [desktop-client.json](desktop-client.json) | Physical desktop capture bounds |

## Actual Windows desktop pixels

![Actual Windows client area](desktop-client.png)

## After resizing the native window to 960×700

![Windows minimum viewport](native-minimum.png)

The colored calibration images are synthetic. This proves neither a Canon capture nor an AI edit.
Windows 10 Pro 22H2, EOS4000D live view/capture, DNPDS620 paper, connected phone QR and the first
voluntary paid OpenAI edit remain untested. The application and archive are unsigned.
