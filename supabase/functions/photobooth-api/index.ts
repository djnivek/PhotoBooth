import { CloudApi } from "./cloud.ts";

const api = new CloudApi({
  url: Deno.env.get("SUPABASE_URL") ?? "",
  serviceKey: Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "",
  accessKeyHash: Deno.env.get("BOOTH_ACCESS_KEY_HASH") ?? "",
  openAIKey: Deno.env.get("OPENAI_API_KEY") ?? "",
  galleryOrigin: Deno.env.get("GALLERY_ORIGIN") ?? "https://djnivek.github.io",
  imageModel: Deno.env.get("OPENAI_IMAGE_MODEL") ?? "gpt-image-2.5-sunburst",
  responseModel: Deno.env.get("OPENAI_RESPONSE_MODEL") ?? "gpt-6-luna",
});
Deno.serve((request) => api.handle(request));
