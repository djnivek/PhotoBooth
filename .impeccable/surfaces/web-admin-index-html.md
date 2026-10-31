---
version: 1
slug: "web-admin-index-html"
primary_target: "web/admin/index.html"
related_targets: ["web/admin/style.css","web/admin/dashboard.js","web/admin/report-model.js","tools/admin-local/server.cjs","tools/admin-local/report.sql"]
---

# Private administration dashboard

Mode: Operate. Target: `web/admin/index.html`, served by `tools/admin-local/server.cjs`.

Kevin needs to open the private Tailscale URL, sign in reliably and inspect the birthday booth's activity, transfer/generation timings and operating limits. This is an ordinary extension of the approved Colors Party identity, delivered code-led under the user's explicit instruction to build and fix it. No new visual world or product claims are introduced.

## Direction contract

THESIS: One truthful evening overview. Reading current counts and diagnosing missing measurements takes priority over decoration; there is no marketing hero or decorative chart.

OWN-WORLD: The approved velvet burgundy, sage, cream paper and gouache wordmark remain recognizable. Compact Bricolage headings, system UI labels, flat separated rows and clear controls adapt the existing world to administration.

STORY: Sign in with a stable local admin code, choose a period, read captured and generated photos, then inspect activity, timings, sharing, themes and current server limits. Missing data remains visibly missing.

FIRST VIEWPORT: Compact brand header and logout; title with connection freshness; period and refresh controls; four aligned count columns; an activity plot and current operating-state panel. Mobile stacks the two panels and keeps each action reachable. Manual refresh retains the last successful report if the network fails.

FORM: Focused monitoring workspace, directly specified extension rather than an open concept exploration. Seed: not applicable. No comp. Existing DESIGN.md and design.json remain unchanged.

FINISH: reviewed and documented. The finish review has disposition ship within this web scope. DESIGN.md and its sidecar are preserved; the reused raster retains its existing provenance.

## Boundaries

Only aggregate read queries. No photos, provider credentials, personal identifiers, paid API calls or configuration writes. Current server budget and storage are separate from period-filtered statistics. Counts are observed events, not unique people or proof of physical printing. Honest empty, unavailable, stale, expired-session and loading states are required.

The login shell is public within the private tailnet; statistics require an HttpOnly session or the existing strong API credential. The optional local credential-file picker never stores the admin code in browser storage or URLs.

## Implemented composition

The login view puts a permanent Code admin label, masked field, explicit reveal control and one opening action beneath the brand. “Où trouver mon code ?” discloses the local file location, explains that this is distinct from the booth code and OpenAI key, and offers a file picker. The dashboard opens with a dated report, filters and JSON export. Four separated count columns lead into activity beside the sage server panel. Timings follow as a comparison table; session, sharing and universe breakdowns use flat rows with tabular figures and proportional bars.

The activity plot groups the last 24 hours by hour and longer periods by Paris calendar day. Its legend names all three series; the expandable “Voir les valeurs” table exposes the same counts without relying on the graphic. The chosen-universe list counts Métamorphose session choices, while the resolved-universe list counts generated images. La Surprise du Chef remains a choice distinct from the resulting universe. There is no photograph browser, settings editor or generation action.

## Browser adaptation

This brief owns the admin adaptation of the incumbent world. It does not replace the native type ramp, display threshold, control dimensions or components in `DESIGN.md` or `.impeccable/design.json`. Those two files remain byte for byte unchanged, including unrelated pre-existing drift.

Cream paper, burgundy ink and primary actions, sand secondary feedback, sage operating state, butter coverage notices and rose errors retain Colors Party. Flat fills, fine separators and modest corners define the workspace; there are no custom shadows or gradients. Controls use 7px corners and the two overview panels use 12px corners. The activity colors distinguish sessions, saved photos and Métamorphoses; labels and the values table preserve meaning beyond color.

The reused local Bricolage Grotesque file loads with `font-display: swap`; headings, count figures and by Estelle use weight 600. Body and control text use the browser's system family, then Segoe UI and sans-serif. The base size is 16px with 1.5 line height; routine rows and controls use 14px, supporting copy 13px and small legends 12px. The dashboard title is 32px, section headings 21px, the login title and main counts 38px. At 780px and below, the title becomes 28px and the login title/counts 32px. At 420px and below, overview headings become 19px. This compact Operate hierarchy is intentional and local to the browser surface. The native font-size detector advisories are not an instruction to enlarge every admin label to booth-touchscreen sizes. The former `Bricolage` CSS alias is corrected to `Bricolage Grotesque`, using the same font file and rendering.

The centered shell is at most 1488px wide. Desktop horizontal padding is 48px, becoming 28px at 1050px and below, then 20px at 780px and below. Desktop has four count columns and an activity/server grid weighted 1.9:1, reduced to 1.35:1 at 1050px. At 780px the counts become a two-by-two summary, the overview and detail grids stack, and the export/filter controls wrap. At 420px the period field fills a row. The wordmark displays at 94×70px on desktop and 76×58px on phones, preserving its proportions.

Buttons and fields are at least 44px tall; the login action is at least 48px. Focus uses a visible 3px velvet outline with a 4px offset. Keep programmatic focus on the dashboard title and the Code admin field. The skip link, permanent labels, live status and alert regions, native select, checkbox and disclosure controls remain functional. The timing table keeps an 800px minimum width inside a focusable horizontal-scroll region; mobile copy explicitly explains the scrolling. Chart-label sizes increase within the SVG viewBox on narrow screens. Color feedback transitions take 160ms; `prefers-reduced-motion` disables transitions. The loading skeleton is static.

## Access and session behavior

The local Node server binds to `127.0.0.1:8787` by default. The configured private route is [the PhotoBooth Tailscale address](https://macmini-de-kevin.tail8f2f33.ts.net/photobooth/). It serves the shell and approved assets without an admin session, while report and health data require authentication. The locally authenticated Supabase CLI reads a fixed query against the expected linked PhotoBooth project; no Supabase or provider credential is sent to the browser. This is a private Mac service, not a publicly hosted admin.

The random admin code is retained in `artifacts/admin-local/access-token`, with owner-only file permissions. An invalid existing code file is rejected rather than silently replaced. The browser accepts pasted input or reads the selected local file for this login, without putting the code in local storage, session storage or URLs. Successful login clears the fields. Five failed attempts trigger a one-minute server login block; the form distinguishes a wrong code, rate limiting and connection failure.

Login creates a random session held in server memory for 12 hours. Its cookie is HttpOnly, SameSite=Strict, scoped to `/photobooth`, and Secure on the Tailscale HTTPS host. A server restart invalidates browser sessions without changing the persistent admin code. Opening or reloading the page checks the session; an expired or unauthorized report clears the displayed data and requests the code again. A restored browser page checks access again. Logout revokes the session and expires the cookie; a network failure leaves an explicit “Déconnexion non confirmée” error instead of claiming success.

Session creation and logout require the matching origin and an admin request header. Unknown hosts and foreign origins are rejected. Existing strong Bearer and Basic credentials remain supported for API clients, but there is no browser Basic challenge. Navigating to the report URL as HTML redirects to the dashboard; the explicit export remains a JSON attachment. Responses disallow caching and framing. Business writes return read-only errors; the login/logout POSTs change authentication state only.

## Refresh, missing data and measurement boundaries

The visible period choices are 24 hours, 7 days and 30 days. Classified trials are excluded by default, with an explicit inclusion checkbox. A filter change replaces the pending request. Successful reports refresh after 30 seconds while the page is visible; hiding the page pauses that timer and returning refreshes immediately. Manual refresh remains available. A report request times out after 30 seconds. The server can reuse a report for 15 seconds for the same filters; the browser still receives no-store responses.

The first read shows loading feedback. A first-read failure shows an actionable unavailable message. A failed later read preserves the last successful figures, restores their original filter values and marks their timestamp as the last available data. It does not relabel old numbers as the newly requested period. The export link uses the last successful report's filters and makes a new authenticated read, so it can contain a newer report than the currently displayed snapshot. Empty activity and breakdowns explain what is absent; missing duration or count measurements use “Non mesuré”. Server-state absence is distinct from zero jobs.

Counts are recorded events: starts are not unique guests, captures are saved photographs, gallery openings are not unique visitors and download clicks are not completed transfers. Results and starts are counted within the selected period independently, including outcomes of sessions that began earlier. Legacy or unclassified events are disclosed. Recorded activity without session starts prompts the version 0.7 coverage explanation; the dashboard does not infer missing historical starts or captures.

Timing rows show mean, median and P95 for measured successful upload, generation and booth-download operations, with measured/successful coverage and unsuccessful outcomes alongside. Missing durations are not zero. Transfer-slot waiting is excluded. Generation measures the complete OpenAI response before storage; generation payload volume is not tracked. These observations do not establish current provider performance under a real event load.

The sage server panel is a separate current snapshot across all sessions, including trials, independent of period filters. It reports the generation switch, reserved starts versus the event ceiling, simultaneous/minute limits, job states and stored objects. Reserved starts are not billed spend. Uncertain results and jobs without an update for five minutes receive explicit warnings; the interface cannot retry them. Storage sums known object sizes and reports unknown-size files; plan quota is not measured. The last measurement timestamp is not a booth heartbeat. Canon connectivity and physical printing are outside this report.

## Sources and asset provenance

Implementation: `web/admin/index.html`, `style.css`, `dashboard.js` and `report-model.js`; local routing, authentication and read boundaries in `tools/admin-local/server.cjs`; the fixed aggregate query in `tools/admin-local/report.sql`.

The admin serves `web/gallery/assets/wordmark.png` and `BricolageGrotesque.ttf` unchanged. The approved wordmark retains its sibling `wordmark.prompt.txt` and embedded provenance; the font's existing `OFL-BricolageGrotesque.txt` remains with it. The lettering is raster artwork, not another font. This extension adds no shipping raster artwork, generated asset, illustration replacement or decision comp. Screenshot JPEGs below are verification evidence, not product assets.

## Evidence and limits

The [finish review](../../docs/reviews/admin-dashboard/impeccable.md) records disposition **ship** with no material fixes required. Its scope is this administration surface. The static detector receipt predates the mechanical font-alias correction; its skip-link black-color finding came from unresolved `__BASE__` stylesheet loading in that static scan. Actual CSS defines the link colors. No detector ignore or global system rewrite was introduced.

Versioned captures are in `docs/evidence/admin-dashboard/`. Heights below are full-page image heights except for the login views; they are not mobile viewport heights.

| Evidence | Captured state and scope |
| --- | --- |
| `login-user.jpg` (1280×720), `login-mobile.jpg` (390×844) | Browser login on desktop and phone-sized viewport. |
| `live-desktop.jpg` (1280×2417), `live-mobile.jpg` (390×3800) | Actual authenticated report: one legacy generation, no observed session starts, and the visible coverage warning. |
| `rich-desktop.jpg` (1440×2737), `rich-mobile.jpg` (390×4352) | Synthetic rich report showing timing coverage, operational warnings and populated breakdowns. |
| `rich-user-1280.jpg` (1280×2797) | Synthetic rich report captured at the actual 1280px browser width. |
| `empty-desktop.jpg` (1440×2090), `empty-mobile.jpg` (390×3252) | Synthetic empty report and truthful missing measurements. |
| `stale-desktop.jpg` (1280×2866) | Synthetic refresh failure with retained values, dated freshness and original filters. |

Implementation browser checks through CUA covered login, period/trial filters, JSON export, failed-refresh retention, logout and reload. The reviewer inspected source and captures and did not independently repeat those interactions. `docs/evidence/admin-dashboard/live-checks.json` records fourteen checks on 31 October 2026 through private Tailscale HTTPS from the serving Mac: reachable login, anonymous-report denial, origin rejection, form login, scoped Secure HttpOnly session, real report/operational aggregates, no-store responses, filtered JSON export, write denial and logout revocation. No paid provider calls or business writes were used.

The live report is sparse; the rich, empty and stale fixture counts are not event outcomes. This documentation pass read implementation and inspected sample captures without repeating browser or API tests. Access from another physical tailnet device remains unverified. Availability depends on the Mac process, Tailscale and its Supabase CLI connection; the checks do not establish unattended uptime, installation of booth 0.7, current Canon/DS620 operation or physical phone behavior. Recheck external-device access during the next device rehearsal, and assess populated real-event timings after 0.7 has collected them.
