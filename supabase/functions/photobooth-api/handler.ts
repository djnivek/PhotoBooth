// Preparation only: no credentials, user data, database or model calls.
export function handleRequest(request: Request): Response {
  const path = new URL(request.url).pathname.replace(/\/$/, "");
  const isHealth = path === "/health" || path === "/photobooth-api/health" ||
    path === "/functions/v1/photobooth-api/health";
  const headers = {
    "Content-Type": "application/json; charset=utf-8",
    "Cache-Control": "no-store",
    "X-Content-Type-Options": "nosniff",
  };
  if (!isHealth) {
    return new Response(JSON.stringify({
      code: "not_implemented",
      message: "Only the preparation health endpoint is available.",
      retryable: false,
    }), { status: 404, headers });
  }
  if (request.method !== "GET") {
    return new Response(JSON.stringify({ code: "method_not_allowed", retryable: false }), {
      status: 405,
      headers: { ...headers, Allow: "GET" },
    });
  }
  return new Response(JSON.stringify({
    service: "photobooth",
    api_version: "0.1.0",
    stage: "preparation",
    production_ready: false,
    session_api_enabled: false,
    image_generation_enabled: false,
  }), { status: 200, headers });
}
