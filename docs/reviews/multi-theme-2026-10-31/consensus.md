# Product decisions after Opus consultation

The complete Opus 5.5 Max consultation accepts the overall design with amendments. Its unedited report and actual model receipt are adjacent files. The earlier broad no-tool attempt hit an output limit and has no verdict; its incomplete receipt is retained separately.

Accepted and implemented:

1. Chef is exclusive in both photo counts. One pose gets one theme; four poses get four distinct active themes when possible, otherwise a balanced plan.
2. Every selected theme is represented, with counts differing by at most one. With three choices one randomly selected theme repeats. The final order is shuffled independently of click order.
3. Capture order remains unchanged. Because the print master is row-major 2x2, duplicate themes occupy diagonal cells. This supersedes the initial proposal to forbid consecutive capture indices: chronological ABBA is correct for diagonal placement.
4. Four distinct broad creative directions are shuffled across the four shots, including repeated themes. This is variety in instructions, not a guarantee of visually different generations.
5. Complete per-pose prompts and IDs are frozen atomically by the server before any generation. Old sessions keep their original scalar prompts. Clients do not send prompts. A missing or invalid multi-theme plan prevents uploads and generations.
6. A single photo replaces its selected theme; four photos allow one to four selected themes. Chef clears explicit choices and explicit choices clear Chef. Removed catalog choices are discarded on refresh. No prompt or picker appears between captures.
7. Identity instructions also preserve glasses, hearing aids, prostheses and personal head coverings. The glass-display setting is non-food and faces remain free of glass/reflections. In the feast setting, people are among oversized scenery, never plated, edible or dressed as food.

Resolution of the proposed animal allowlist:

Kevin explicitly asks the model to choose costumes freely, without prescribing particular examples. Therefore no closed animal/species list is imposed. The prompt instead requires gentle, respectful animal-inspired fabric costumes, human faces/anatomy, and choices independent of physical appearance, age or origin. This preserves the review's intention while respecting the user's creative-freedom requirement. The implementation review agrees with declining a positive allowlist and recommends explicitly excluding animals commonly used as personal insults. That negative exclusion is now included, without prescribing any actual costume. The final focused review confirms all four amendments and closes the earlier disagreement; product and prompt consensus is reached.

Nine bundled and server themes: Colors Party, Halloween, Dans les étoiles, Pays imaginaire, Petits lutins, Disco velours, Drôles de bêtes, Sous cloche and À croquer. Chef is a separate option. Public catalog metadata contains no prompts. The 20-theme test catalog is test-only, not twenty invented production themes.


Implementation-review follow-up:

- Keep costume freedom, add the requested negative exclusion of insulting animal comparisons.
- Clarify creative direction 4: preserve faces and bodies, while outfits remain changeable.
- Keep the existing unicorn guest copy accurate by including unicorns as background scenery, never costumes or replacements for people.
- Replace the server test's fixed count with source-relative metadata checks and catalog constraints, so adding a valid theme does not break the test.
- Cover mixed and future themes in the gallery DOM test and in Chromium's four-photo/progressive fixtures. The public gallery pairs assets by capture index; unknown themes use existing generic artwork and never authorize generation.

Final sign-off: SHIP conditional on the final Chromium and Windows CI checks. The remaining hardware and real-portrait gates are not presented as completed. See `claude-final-signoff.md` and its receipt.
