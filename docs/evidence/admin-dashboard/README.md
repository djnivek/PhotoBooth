# Private admin dashboard evidence

Captured on 31 October 2026 with the supported computer-use browser. No guest photos, credentials, provider calls or business writes were used.

## Before and after

Before: `/api/report` required a browser HTTP Basic challenge and returned raw JSON. The in-app browser reported `ERR_INVALID_AUTH_CREDENTIALS`; the previous [activation receipt](../mac-admin/service.json) explicitly records that authenticated browser rendering was unverified. The code also changed on every server restart. There was no dashboard to screenshot.

After: the old browser URL redirects to a regular login page. A stable local code opens the aggregate dashboard through an HttpOnly session. The existing private Tailscale route and CLI account remain in use.

| Capture | Content | Raster width |
| --- | --- | --- |
| [login-user.jpg](login-user.jpg) | Login at the actual browser viewport | 1280 |
| [login-mobile.jpg](login-mobile.jpg) | Phone login | 390 |
| [live-desktop.jpg](live-desktop.jpg) | Actual authenticated Tailscale report | 1280 |
| [live-mobile.jpg](live-mobile.jpg) | Actual authenticated report at phone width | 390 |
| [rich-desktop.jpg](rich-desktop.jpg) | Synthetic populated report, timings and themes | 1440 |
| [rich-user-1280.jpg](rich-user-1280.jpg) | Synthetic populated report, corrected explicit actual width | 1280 |
| [rich-mobile.jpg](rich-mobile.jpg) | Synthetic populated report at phone width | 390 |
| [empty-desktop.jpg](empty-desktop.jpg), [empty-mobile.jpg](empty-mobile.jpg) | Synthetic zero-event report and unavailable operations | 1440 / 390 |
| [stale-desktop.jpg](stale-desktop.jpg) | Synthetic failed refresh, retained figures and original filter | 1280 |

Synthetic captures are layout/state evidence, not event performance. The live snapshot contains one historical generation with no observed session starts. The coverage notice states that precise booth measurements require client 0.7. Server-wide stock/job/budget aggregates are separate from period-filtered event statistics. Source fixture: `tests/fixtures/admin-report.cjs`.

The browser verified incorrect-code feedback, file-based sign-in without printing the code, period and fixture filters, JSON download (contents checked), stale-data/filter restoration, logout, reload with the session, and mobile document width equal to 390px. No warning/error console entries occurred on the actual dashboard. The timing table intentionally scrolls horizontally on phones and exposes a visible instruction.

[37 Node checks](node-tests.tap) passed, including 17 admin authentication, query and report-model checks.

[14 live HTTP checks](live-checks.json) exercise valid TLS, login, Secure/HttpOnly/scoped cookies, real report, export, anonymous/foreign-origin denials, write rejection and logout. These checks ran from the serving Mac through its private HTTPS origin. Another physical tailnet device, camera and printer are not covered.

[Impeccable review](../../reviews/admin-dashboard/impeccable.md): ship. One static detector pass is recorded in [design-findings.json](design-findings.json). The CSS was scanned separately because the server-injected `__BASE__` placeholder cannot resolve in the static HTML scan. The font alias was normalized to the documented Bricolage Grotesque name without changing its file or rendering. Compact web font-size advisories and the resulting unstyled-template color advisory are documented, not silently suppressed. Existing gallery-only ignores are unchanged. No new raster asset was made; the approved wordmark and its provenance remain untouched.
