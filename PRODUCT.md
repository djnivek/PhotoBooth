# Product

<!-- impeccable:product-schema 1 -->

## Platform

native desktop (Windows target; macOS rehearsal with the same Avalonia UI). The skill taxonomy has no Windows desktop value; do not infer iOS/Android patterns.

## Stack

Delegated by Kevin: retain the existing C# domain core; use Avalonia for a native application runnable on the Mac now and self-contained Windows x64 delivery. Windows-only adapters control Canon EDSDK and the print spooler.

## Users

Estelle and the guests at her birthday on 31 October 2026. Kevin operates the booth. No account or technical knowledge required from guests.

## Product Purpose

A joyful, personal birthday photo booth. Choose 1 or 4 photos and Original or Surprise; live view to pose, countdown, real captures, result preview, optional one-sheet 4x6 print and QR downloads.

## Operating Context

Canon EOS 4000D, DNP DS620, 4x6 stock, Windows 10 Pro 22H2. Tonight the rehearsal PC has no camera/printer. The actual booth has Internet but no Codex and must run a downloaded executable folder. A mock mode must exercise the same imaging pipeline without paid API calls.

## Capabilities and Constraints

One session at a time. Full screen with administrator PIN exit. An actual SDK and DNP driver are needed for hardware use. OpenAI image quality xhigh is explicit; automated tests must never call paid APIs. A real API rehearsal is an explicit operator action. No promises of physical printing until tested.

## Brand Commitments

French, festive, for Estelle. A pun on her name is requested. Proposed working name: Estell’Art (inferred pending optional preference). No age or personal photos supplied.

## Evidence on Hand

Portable C# core and 26 preparation checks. No UI currently exists. Synthetic photos may be created for simulation, clearly marked. No actual Canon, DNP or OpenAI image-output evidence exists yet.

## Product Principles

- Make the guest’s next action obvious and large enough to touch.
- Keep the live picture visible during the countdown.
- Preview and print the exact same composed image.
- Never turn an uncertain API or printer outcome into an automatic duplicate.
- Keep technical operating choices in operator settings. Kevin explicitly requests no simulation/rehearsal labels in the interface; the guest option is “Surprise”.

## Open Decisions

Name preference and optional visual workflow question were presented; independent implementation continues from the explicit brief while waiting. Screen resolution and portrait/landscape installation are not known: support a 1280x800 landscape baseline and graceful smaller-window scaling.
