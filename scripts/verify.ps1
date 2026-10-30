$ErrorActionPreference = 'Stop'
Push-Location (Join-Path $PSScriptRoot '..')
try {
    dotnet build PhotoBooth.slnx --configuration Release --disable-build-servers -m:1
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    dotnet run --project tests/PhotoBooth.Core.Tests --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Domain checks failed' }
    dotnet run --project tests/PhotoBooth.Imaging.Tests --configuration Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Imaging checks failed' }
    deno check supabase/functions/photobooth-api/index.ts
    if ($LASTEXITCODE -ne 0) { throw 'Edge function type check failed' }
    deno lint supabase/functions/photobooth-api
    if ($LASTEXITCODE -ne 0) { throw 'Edge function lint failed' }
    deno test supabase/functions/photobooth-api/handler_test.ts
    if ($LASTEXITCODE -ne 0) { throw 'Edge function checks failed' }
}
finally { Pop-Location }
