**Verdict: SHIP.** I found no code defect in the paths I traced. One prompt change (Medium) is worth making before the event; it only touches the server catalog file, so no client rebuild is needed. I ran no tests and generated no images. The results in VALIDATION.md are the implementer's own reports.

## Findings

**1. Medium (prompt): the "Drôles de bêtes" resolution only partly holds.** `supabase/functions/photobooth-api/styles.json:93`
- **Problem (from reading the prompt, not from a render):** the prompt asks for "une inspiration animale pour chaque personne… sans te baser sur l'apparence physique, l'âge ou l'origine". The model can only tell people apart by what it sees in the photo. Choosing a different animal per person is exactly how appearance-based choices happen, so the instruction can't really be enforced.
- **Guests don't opt in:** four-photo Chef picks 4 of the 9 themes, so about 44% of those sessions include this theme.
- **Why it matters:** declining the closed allowlist is right, because the user asked for creative freedom. But the excluded "insult" animals from the accepted amendment were dropped along with it, and that exclusion prescribes no costume. Creative freedom never required dropping it.
- **Minimal fix:** add one sentence: "Écarte les animaux employés comme moqueries envers une personne (singes, cochons, vaches, ânes)." Optionally, ask for one animal inspiration shared by the whole group instead of one per person.

**2. Low: creative direction 4 contradicts costume-led themes.** `style-plans.ts:67`
- "…sans modifier les personnes" can be read as "don't change the people's clothes either".
- `freezePlan` gives each of the four directions to exactly one pose (`:73-76`). So every four-photo session has one pose that may lose its costume, which is the whole point of Drôles de bêtes and Halloween.
- **Fix:** "…sans modifier les visages ni les corps."

**3. Low: "Pays imaginaire" copy promises unicorns the prompt no longer asks for.**
- The guest text says "Les licornes sont de la partie." and "Les licornes lissent leur crinière." (server `styles.json:46,50`; client `src/PhotoBooth.Core/styles.json:43,47`).
- The prompt at `styles.json:54` doesn't mention them. Unicorns were scenery companions, not a costume, so the no-fixed-costume rule didn't require removing them.
- **Fix:** put them back in the prompt as scenery companions, or change the copy.

**4. Low: the server catalog's own rules aren't tested.**
- The only server-catalog test checks `length === 9` (`cloud_test.ts:458`). It fails on any legitimate addition made by following UNIVERS.md, and it checks no ID rules.
- A duplicate ID makes `balancedPlan` throw (`style-plans.ts:33`) for every Chef session. Chef is the default, and the 400 error arrives after the guest has already posed.
- An entry with the ID `chef` can end up in a frozen plan and is only rejected later with a 503 (`cloud.ts:212`).
- An empty `prompt` produces paid images with no theme.
- The booth rejects such a catalog, but Chef still draws from the server file.
- **Fix:** replace the count check with a test of the rules: unique IDs, valid ID format, not `chef` or `mix`, non-empty prompt, and the same guest-text limits as `StyleCatalog.Parse`.

## Checked and correct
- **Allocation:** every chosen theme is used, counts differ by at most one, repeats land on the diagonals, and the order is random (`style-plans.ts:32-50`, `StyleSelection.cs:20-54`). The sheet really is filled row by row (`PrintLayout.cs:14-15`). Chef is exclusive on both client and server.
- **Frozen plan:** the client copies and sorts the selection (`Session.cs:23`). The server saves the per-pose plan in a single insert, and duplicate requests can't replace it (`cloud.ts:497-512`). The client rejects a changed plan before any upload or generation (`BoothCloudClient.cs:65-74`). Retries keep the same plan (`Session.cs:100`).
- **Damaged plan:** rejected at `cloud.ts:544`, before the quota claim at `:562`. The provider call and the generation metric both use that pose's own theme and prompt (`:338`, `:318`).
- **Old sessions and old clients:** sessions created before the migration keep their single original prompt (`:210`, `:510`, `:516`). A 0.8 client using Chef with four photos receives `style:"mix"`; its old code accepts it, and its print falls back to the "La Surprise du Chef" label (removed lines in changes.diff:380-382, 500, 511). Authentication and quotas are unchanged.
- **Theme picker:** tapping a tile only updates the selection and never rebuilds the screen, so scroll position and focus are kept (`PartyScreens.cs:129`). At the four-theme limit, other tiles are disabled while the chosen ones can still be removed (`:177`). The footer stays pinned outside the scroll area. CPU and memory cost is small: at most 24 orderings to check, cached artwork, no polling.

## Product and prompt consensus
Agreed on: allocation, Chef, diagonal order, server-frozen per-pose prompts, the extra identity rules, Sous cloche, À croquer, and declining a closed animal allowlist.

**Not agreed on one point:** also dropping the exclusion of insult animals. I recommend restoring it (finding 1). If Kevin keeps the current wording, the diverse-subject check in gate 2 must pass before the theme is used.

## Manual gates (not code defects)
1. **Deployment order:**
   - Apply migration `20261031175047` first. It was only tested in a rolled-back transaction and there is no committed SQL test.
   - Then deploy function 0.9.0, then client 0.9.0.
   - If the function goes first, every new `POST /sessions` returns 503 because the columns don't exist yet, including for old clients.
   - If the client goes first, multi-theme sessions fail, and so does sharing their originals (`PublishAsync` → `EnsureSession`).
2. **First paid run:**
   - Check each pose's prompt and metric for one three-theme session.
   - Kevin should judge likeness on all 9 themes, in particular glass and reflections in Sous cloche, people's placement in À croquer, and Drôles de bêtes on a varied group (skin tones, body sizes, ages, head coverings, glasses, hearing aids).
3. **Kiosk hardware:**
   - Try real touch taps, drags and keyboard on the 20-theme picker, on the old i7 under Windows 10.
   - The headless test triggers tile clicks directly (App.Tests `Program.cs:330`) and uses only one simulated mouse click. It never uses touch or keyboard on the tiles.
4. **Guest gallery:** check how it displays `style:"mix"`. Its code isn't in this snapshot.