# Third-party software

EstellArt is a personal application. Its self-contained Windows package contains the .NET
runtime and libraries below. Their upstream licenses and notices continue to apply.

| Component | License / source |
| --- | --- |
| .NET runtime and libraries | MIT and third-party notices: https://github.com/dotnet/runtime |
| Avalonia 12.1.4 | MIT: https://github.com/AvaloniaUI/Avalonia |
| SkiaSharp 3.119.4 and native Skia | MIT and native third-party licenses: https://github.com/mono/SkiaSharp |
| QRCoder 1.8.1 | MIT: https://github.com/codebude/QRCoder |
| FC.SDK 3.2.1021 | MIT, copy in licenses/FC.SDK-MIT.txt: https://github.com/SharpAstro/FC.SDK |
| LibUsbDotNet 2.2.75 (FC.SDK dependency; direct USB backend unused) | LGPL: https://github.com/LibUsbDotNet/LibUsbDotNet |
| Microsoft.Extensions.Logging.Abstractions | MIT: https://github.com/dotnet/runtime |
| Bricolage Grotesque | SIL Open Font License1.1, copy bundled: https://github.com/ateliertriay/bricolage |

No proprietary Canon EDSDK binaries or DNP printer drivers are included. The default Canon
adapter uses the Windows WPD driver already on the computer. The optional EDSDK adapter
requires separately acquired Canon software and its applicable license.
