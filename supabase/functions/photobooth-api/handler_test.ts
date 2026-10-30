import { handleRequest } from "./handler.ts";

function assert(value: unknown, message: string) {
  if (!value) throw new Error(message);
}

Deno.test("health is public and explicitly not production-ready", async () => {
  for (const path of ["/health", "/photobooth-api/health", "/functions/v1/photobooth-api/health/"]) {
    const response = handleRequest(new Request(`https://example.invalid${path}`));
    assert(response.status === 200, "health must respond");
    assert(response.headers.get("Cache-Control") === "no-store", "no stale readiness");
    const body = await response.json();
    assert(body.production_ready === false && body.image_generation_enabled === false &&
      body.session_api_enabled === false, "never imply functional photo processing");
    assert(Object.keys(body).length === 6, "expose only intended metadata");
  }
});

Deno.test("business routes are unavailable and cannot incur model usage", async () => {
  for (const path of ["/sessions", "/sessions/example/complete", "/downloads/resolve", "/secret/health"]) {
    const response = handleRequest(new Request(`https://example.invalid${path}`, {
      method: "POST", body: JSON.stringify({ arbitrary: "input" }),
    }));
    assert(response.status === 404, "no business operation should be activated");
    assert((await response.json()).retryable === false, "must not encourage retries");
  }
});

Deno.test("health refuses mutation methods", () => {
  for (const method of ["POST", "PUT", "DELETE", "PATCH"]) {
    const response = handleRequest(new Request("https://example.invalid/health", { method }));
    assert(response.status === 405 && response.headers.get("Allow") === "GET", "read only");
  }
});
