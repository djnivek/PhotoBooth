# Product

<!-- impeccable:product-schema 1 -->

## Platform

Native Windows desktop, rehearsed on macOS with Avalonia. No iOS or Android surface.

## Stack

C# and .NET 10, Avalonia 12.1.4, local Skia imaging, Windows WPD/EDSDK camera adapters and GDI printing. Supabase Edge Function stores photos and calls OpenAI. Self-contained Windows x64 executable delivered through the private repository.

## Users and purpose

Estelle and guests at her birthday on 31 October 2026. Kevin operates the booth. Select one or four photos, Au naturel or Métamorphose, pose in live view, then 3, 2, 1 before each capture. Review the exact 4×6 sheet, optionally print, download using the QR.

## Operating context

Windows 10 Pro 22H2, Canon EOS 4000D, DNP DS620, 4×6 stock. Old third-generation i7 and 8 GB RAM. Rehearsal PC has neither camera nor printer. The actual booth cannot run Codex. No paid API calls in automated tests.

## Brand commitments

Kevin explicitly names the application Colors Party by Estelle and approves the matte gouache illustrations, sage, butter, coral and velvet burgundy palette. Keep that artwork; subtle 1980s cues are optional. French guest copy has no AI jargon, simulation labels or dash punctuation. Métamorphose offers Colors Party, Halloween, Dans les étoiles, Pays imaginaire, Petits lutins and Disco velours, plus La Surprise du Chef.

## Product principles

Large touch controls. Live image stays visible through countdown. A new camera preview and successful preparation precede each countdown. Transfer must complete before the next pose. Never retry an ambiguous shutter, paid generation or print submission automatically. Printed bytes match the verified preview sheet.

## Dynamic universes

Six built-in choices are immediately available. Fetch public metadata once in the background on startup, keep it only in memory, and refresh explicitly in settings. Opening the picker makes no request. Do not replace choices under a guest. Backend additions, removals, order and scene prompts can change without rebuilding the app. Unknown artwork uses a stable local fallback. Freeze scene prompt and chosen universe for an existing session.

## Operator access

Settings button visible in windowed mode. Optional four-character code. Entering fullscreen keeps settings open. Once dismissed, hold the lower-left corner for three seconds; authenticate if a code is configured. Short hold, movement away or cancellation must not open settings. Windows system shortcuts are outside this application lock.

## Evidence and limits

Native headless UI and mocked camera/cloud/printing tests exist. Windows package execution is checked by CI separately. No current hardware validation on EOS 4000D or DS620, and no paid OpenAI render in the automated suite. WPD autofocus completion is not proven on this camera; explicit verified manual focus is the available fallback. The official SDK is not provided. A scene prompt cannot guarantee identical generated facial pixels. Supabase plan changes remain deferred until physical rehearsal succeeds.
