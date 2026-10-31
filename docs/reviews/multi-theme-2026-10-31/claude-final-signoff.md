**Verdict: SHIP**, once the pending Chromium run and the final Windows CI pass. All four findings are resolved and the product and prompt decisions are agreed. I read only AMENDMENTS.md. I didn't run tests or look at generated portraits.

**The four findings: all resolved**
1. **Insult animals:** The sentence excluding them is back, and "notamment" keeps the list open. Creative freedom, no fixed animal list, and one animal per person with faces and bodies kept are all unchanged.
2. **Creative direction 4:** "sans modifier les visages ni les corps" protects faces and bodies and no longer blocks costumes.
3. **Unicorns:** They're back in the prompt as background scenery that never hides or replaces anyone. The guest text is accurate again.
4. **Catalog tests:** The count check now compares against the source file. The new test covers unique IDs, ID format, the reserved `chef`/`mix` IDs, non-empty prompts and guest-text limits. Allowing an empty catalog is a deliberate choice and the reason is given. I can't confirm the numeric limits match `StyleCatalog.Parse`, because the C# code isn't in the diff.

**Gallery check (manual gate 4): closed.** The new DOM test covers the original/transformed pairs and downloads for mix, new and future themes. The "Métamorphose" caption fallback isn't tested. It rests on the implementer's reading of viewer.mjs, and it's cosmetic only.

**Remaining concerns (none blocking)**
1. **The new test can fail on normal French (low).** `guestCopy` uses the `i` flag, so `\b(AI|IA)\b` also matches "j'ai" and "l'ai". A future phrase like "J'ai sorti mes plumes." would fail on an assert that has no message. Make the AI/IA match case-sensitive, and do the same in `StyleCatalog.Parse` if it uses this rule.
2. **Possible extra reserved ID (minor).** The gallery mock now returns `style:"original"` for sessions without a theme. If production does the same, reserve `original` too.
3. **Release order.** The client `styles.json` version also moved to `.3`, so ship the ZIP built from this revision. The earlier exact-ZIP run doesn't cover it. The order stays: function deploy, metadata re-check, then client.
4. **Additions to the first paid run (gate 2).** The sentence I asked for names the four animals, and naming things in a "don't" instruction can make image models more likely to draw them. On the first paid runs:
   - Check that none of the four appear.
   - On the varied group, look for animals picked by body size, such as an elephant or hippo for larger guests.

**Product and prompt consensus: reached.** Agreed on:
- theme allocation, Chef, the diagonal order and the per-pose prompts frozen on the server
- the identity rules, Sous cloche and À croquer
- creative freedom with no fixed animal list, plus the restored insult-animal exclusion
- unicorns as scenery, the new direction 4 wording, and allowing an empty catalog

The one earlier disagreement is closed. Not switching to a single animal for the whole group is acceptable. The risk that remains is checked in gate 2.

**Manual gates still open**
- Deploy the function, then the client. The migration was correctly applied first.
- The first paid run, which is where portrait quality gets judged.
- Physical Canon/DNP printing on Windows 10, and touch and keyboard on the old i7.