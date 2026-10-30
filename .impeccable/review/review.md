## verdict

1. Resolved — capture signature: `capture-flash.png` visibly shows a restrained translucent white veil while retaining the framing image and readable “Clic !” badge. Source confirms opacity 0.22, a single 170 ms timer, and a transition guard preventing repeated flashes during the same Capturing state. The countdown pulse remains intact.
2. No regressions identified in the five regenerated required captures; all six captures are valid. This ship verdict covers the scored correction only, within native desktop rehearsal scope; it is not a new whole-surface or hardware-production validation.

## remaining

clear

disposition: ship

# Addendum — cloud settings and sharing controls

disposition: fix

## persistence
The surface contract remains present. Reviewed the regenerated `result.png` (1280×800), `settings.png` (690×680 dialog) and `unlock.png` (480×360 dialog), plus the current MainWindow.cs. These captures are valid. No new-state result capture at 1024×768 was supplied, so that size is not visually certified by this addendum. The fixed-size dialogs fit that viewport geometrically; actual placement/window decorations remain unverified. Online QR, Windows hardware and the undeployed gallery are outside this evidence.

## fidelity
TYPE, MATERIAL and GROUND remain consistent. The local result preserves the photograph-first composition and correctly says sharing is disabled. Settings use ordinary native controls and scrolling, without clipping the visible text; the PIN/fullscreen controls below the fold require a scrolled capture. The unlock actions are legible. Input instructions rely entirely on dim placeholders in both dialogs: the connection code and exit code are harder to read than adjacent text, and their labels disappear during entry. The source also introduces a truth inconsistency: disabling cloud reports “Répétition locale activée” even when real Canon/printing settings remain enabled.

## ceiling
No new decorative treatment needed. Improve field identification and operator-state clarity; retain the existing typography, palette and compact dialogs.

## material_fixes
1. Give the connection code and exit PIN persistent visible labels, using the normal readable foreground; ensure placeholder contrast is at least 4.5:1. In particular the current dim “Code de connexion…” and “Votre code” are not adequate primary instructions.
2. Replace the cloud-disable confirmation with the exact affected state, e.g. “Connexion en ligne désactivée. Aucun appel OpenAI payant.” Do not announce a full local rehearsal while physical camera/printing can remain active.

## keep
Keep the explicit local/real mode footer, stored-sheet preview, clear sharing-disabled message, native scrolling and readable yellow primary action. Before final handoff, capture the settings bottom, unlock error and result at 1024×768; those states have not been visually demonstrated here. A synthetic connected-state capture can verify QR layout without claiming successful online publication.

# Follow-up — settings and sharing controls

## verdict

1. Partial — persistent labels now identify the connection code and exit PIN clearly in `settings.png`, `settings-bottom.png` and `unlock.png`. However, the original low-contrast placeholders remain unchanged, including the unique instruction “pas la clé OpenAI”; the requested placeholder contrast correction is still unresolved. Remove the now-redundant placeholders and move “pas la clé OpenAI” into the visible connection label/helper, or explicitly style placeholders to meet 4.5:1.
2. Resolved — source now reports “Connexion en ligne désactivée. Aucun appel OpenAI payant.” without falsely announcing that physical camera/printing are disabled.
3. No introduced layout regression observed: `settings-bottom.png` exposes the fullscreen and close actions; `unlock-error.png` shows the full error and both actions inside the enlarged dialog; `result-1024x768.png` keeps the sheet, print confirmation, sharing-disabled state and new-session action readable without clipping. These are native local-state observations only, not connected QR, Windows hardware or deployed-gallery validation.

## remaining

Resolve or remove the dim placeholders; preserve the new persistent labels. One targeted dialog recapture is sufficient after that change.

disposition: fix

# Final follow-up — native local states

## verdict

1. Resolved — regenerated `settings.png`, `settings-bottom.png` and `unlock-error.png` show all three code fields without dim placeholders. Persistent labels remain readable; “pas la clé OpenAI” is now part of the visible connection label. Source confirms removal from connection, new PIN and unlock inputs.
2. Resolved — the previously corrected cloud-disable confirmation remains accepted. No regression introduced by the final label/placeholder change is visible in the reviewed dialogs.
3. This ship verdict covers the scored corrections and observed native local states only. It does not certify connected QR sharing, deployed web gallery, physical printing or Canon/Windows operation.

## remaining

clear

disposition: ship
