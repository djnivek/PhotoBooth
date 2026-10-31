# Private Mac statistics access

Source base: `e602b400c9cd6bceb5c270259b0fb631b1441032`. The service activation exposed two real CLI compatibility problems: `db query` rejects `--project-ref`, and the installed CLI returns a JSON row array while the previous parser only accepted an object with `rows`. The server now validates the linked PhotoBooth project before execution and reads both formats.

HTTP Basic login uses the same existing strong local admin token as Bearer. It does not use the booth or OpenAI key. The server still accepts only allowed hosts/origins, binds to loopback and serves read-only requests.

[Live receipt](service.json): the persistent LaunchAgent and private HTTPS route returned a real report with fixtures excluded; unauthenticated access and incorrect passwords returned 401, a foreign origin 403, and POST 405. TLS validation stayed enabled. Existing Tailscale routes were preserved and public Funnel was not enabled. No paid OpenAI request or database mutation was made.

Five Node tests passed, including browser credentials, hostile hosts/origins, write refusal, unlinked/wrong-project rejection, CLI arguments and both response formats. The Host check uses node:http because fetch normalizes that header.

A real Chrome tab displayed the expected native username/password dialog with the exact private HTTPS origin. No token was entered or recorded in browser evidence. The authenticated report was verified over HTTPS, not visually inside the browser. The embedded browser does not handle this Basic-auth challenge and returned ERR_INVALID_AUTH_CREDENTIALS. Another tailnet device is still a separate user check.

This change adds no dashboard layout, application release or frontend assets. The live surface remains the existing JSON report. See [operator instructions](../../ADMIN_LOCAL.md) for the URL, private token path and service lifecycle.
