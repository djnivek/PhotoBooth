#!/bin/sh
set -eu
export AVALONIA_TELEMETRY_OPTOUT=1
cd "$(dirname "$0")/.."
dotnet_command="${PHOTOBOOTH_DOTNET:-dotnet}"
"$dotnet_command" build PhotoBooth.slnx --configuration Release --disable-build-servers -m:1
"$dotnet_command" run --project tests/PhotoBooth.Core.Tests --configuration Release --no-build
"$dotnet_command" run --project tests/PhotoBooth.Imaging.Tests --configuration Release --no-build
"$dotnet_command" run --project tests/PhotoBooth.Runtime.Tests --configuration Release --no-build
"$dotnet_command" run --project tests/PhotoBooth.Windows.Tests --configuration Release --no-build
"$dotnet_command" run --project tests/PhotoBooth.Cloud.Tests --configuration Release --no-build
"$dotnet_command" run --project tests/PhotoBooth.App.Tests --configuration Release --no-build
deno check supabase/functions/photobooth-api/index.ts
deno lint supabase/functions/photobooth-api
deno test supabase/functions/photobooth-api/cloud_test.ts supabase/functions/photobooth-api/viewer_test.ts
node --test tests/viewer.test.mjs tests/gallery.test.cjs tests/admin.test.cjs tests/admin-model.test.cjs
