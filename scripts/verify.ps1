$ErrorActionPreference = 'Stop'
$env:AVALONIA_TELEMETRY_OPTOUT = '1'
Push-Location (Join-Path $PSScriptRoot '..')
try {
    dotnet build PhotoBooth.slnx --configuration Release --disable-build-servers -m:1
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    dotnet run --project tests/PhotoBooth.Core.Tests --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Domain checks failed' }
    dotnet run --project tests/PhotoBooth.Imaging.Tests --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Imaging checks failed' }
    dotnet run --project tests/PhotoBooth.Runtime.Tests --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Runtime checks failed' }
    dotnet run --project tests/PhotoBooth.Windows.Tests --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Print preparation checks failed' }
    dotnet run --project tests/PhotoBooth.Cloud.Tests --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Cloud client checks failed' }
    dotnet run --project tests/PhotoBooth.App.Tests --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'UI checks failed' }
    deno check supabase/functions/photobooth-api/index.ts
    if ($LASTEXITCODE -ne 0) { throw 'Edge function type check failed' }
    deno lint supabase/functions/photobooth-api
    if ($LASTEXITCODE -ne 0) { throw 'Edge function lint failed' }
    deno test supabase/functions/photobooth-api/cloud_test.ts supabase/functions/photobooth-api/viewer_test.ts
    if ($LASTEXITCODE -ne 0) { throw 'Edge function checks failed' }
    node --test tests/viewer.test.mjs tests/gallery.test.cjs tests/admin.test.cjs tests/admin-model.test.cjs
    if ($LASTEXITCODE -ne 0) { throw 'Gallery checks failed' }
}
finally { Pop-Location }
