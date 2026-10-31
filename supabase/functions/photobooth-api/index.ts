import { CloudApi } from "./cloud.ts";

declare const EdgeRuntime: { waitUntil(task: Promise<unknown>): void };

const api = new CloudApi({
  url: Deno.env.get("SUPABASE_URL") ?? "",
  serviceKey: Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "",
  accessKeyHash: Deno.env.get("BOOTH_ACCESS_KEY_HASH") ?? "",
  openAIKey: Deno.env.get("OPENAI_API_KEY") ?? "",
  galleryOrigin: Deno.env.get("GALLERY_ORIGIN") ?? "https://colors-party-estelle.vercel.app",
  imageModel: Deno.env.get("OPENAI_IMAGE_MODEL") ?? "gpt-image-2.5-sunburst",
}, fetch, task => EdgeRuntime.waitUntil(task));
Deno.serve((request) => api.handle(request));
