param(
    [ValidatePattern('^\d+\.\d+\.\d+(-[A-Za-z0-9.-]+)?$')][string]$Version = '0.10.0',
    [string]$OutputRoot = 'artifacts/package'
)
$ErrorActionPreference = 'Stop'
$env:AVALONIA_TELEMETRY_OPTOUT = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
if (-not $IsWindows) { throw 'Packaging validation must run on Windows with PowerShell 7.' }
Push-Location (Join-Path $PSScriptRoot '..')
try {
    $output = [IO.Path]::GetFullPath($OutputRoot)
    $stage = Join-Path $output ('.staging-' + [Guid]::NewGuid().ToString('N'))
    $zip = Join-Path $output "ColorsParty-$Version-win-x64.zip"
    if (Test-Path -LiteralPath $zip) { throw "Package already exists: $zip. Use a fresh OutputRoot." }
    New-Item -ItemType Directory -Path $stage -Force | Out-Null
    dotnet publish src/PhotoBooth.App/PhotoBooth.App.csproj --configuration Release --runtime win-x64 --self-contained true `
        --output $stage --disable-build-servers -m:1 -p:Version=$Version -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
        -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false
    if ($LASTEXITCODE -ne 0) { throw 'Windows publication failed' }
    Copy-Item docs/DEMARRAGE_WINDOWS.txt (Join-Path $stage 'LISEZ-MOI.txt')
    Copy-Item THIRD_PARTY_NOTICES.md $stage
    Copy-Item docs/KIOSQUE_WINDOWS.md $stage
    Copy-Item scripts/New-KioskConfiguration.ps1 $stage
    Copy-Item licenses $stage -Recurse
    Copy-Item src/PhotoBooth.App/Assets/OFL-BricolageGrotesque.txt (Join-Path $stage 'licenses')
    New-Item -ItemType Directory -Path (Join-Path $stage 'canon') | Out-Null
    Set-Content (Join-Path $stage 'canon/README.txt') -Encoding utf8 -Value 'Connexion Canon par defaut : pilote Windows WPD, sans SDK. Cette option ne demande aucune DLL ici. L option SDK officiel des Reglages est reservee a un SDK x64 acquis aupres de Canon, avec ses dependances et sa licence.'
    if (Get-ChildItem $stage -Recurse -File | Where-Object { $_.Name -match '^\.env|credential|provisioning|access-key|server-secrets' }) { throw 'Credential-like file in package' }
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal

    # Validate what a user downloads and extracts, rather than only the publish directory.
    $unpacked = Join-Path $output ('.download-test-' + [Guid]::NewGuid().ToString('N'))
    Expand-Archive -LiteralPath $zip -DestinationPath $unpacked
    $diagnostics = Join-Path $output 'diagnostics'
    New-Item -ItemType Directory -Path $diagnostics -Force | Out-Null
    $executable = Join-Path $unpacked 'ColorsParty.exe'
    foreach ($mode in @('--self-test', '--smoke-ui')) {
        $process = Start-Process -FilePath $executable -ArgumentList @($mode, ('"' + $diagnostics + '"')) -Environment @{PHOTOBOOTH_SMOKE_DESKTOP_CAPTURE='1'} -PassThru
        if ($mode -eq '--smoke-ui') { & (Join-Path $PSScriptRoot 'capture-window.ps1') -ApplicationProcess $process -OutputDirectory $diagnostics }
        if (-not $process.WaitForExit(90000)) { $process.Kill(); throw "Packaged executable timed out: $mode" }
        if ($process.ExitCode -ne 0) { throw "Packaged executable failed: $mode, exit $($process.ExitCode). Inspect $diagnostics." }
    }
    $selfTest = Get-Content (Join-Path $diagnostics 'self-test.json') -Raw | ConvertFrom-Json
    $ui = Get-Content (Join-Path $diagnostics 'native-ui.json') -Raw | ConvertFrom-Json
    $minimumUi = Get-Content (Join-Path $diagnostics 'native-minimum-ui.json') -Raw | ConvertFrom-Json
    if (-not $selfTest.success -or $selfTest.cases.Count -ne 4 -or -not $ui.native_window_opened -or
        -not $ui.controls_inside_client -or -not $minimumUi.controls_inside_client) { throw 'Missing executable validation evidence' }
    $digest = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    Set-Content -LiteralPath "$zip.sha256" -Encoding ascii -Value ($digest + '  ' + [IO.Path]::GetFileName($zip))
    [ordered]@{version=$Version; artifact=[IO.Path]::GetFileName($zip); sha256=$digest; bytes=(Get-Item $zip).Length;
        source_commit=(git rev-parse HEAD); windows_self_test=$true; native_window_opened=$true;
        hardware_validated=$false; openai_called=$false} | ConvertTo-Json | Set-Content (Join-Path $diagnostics 'package.json') -Encoding utf8
    foreach($temporary in @($stage,$unpacked)) {
        $resolved=[IO.Path]::GetFullPath($temporary)
        if(-not $resolved.StartsWith($output+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) {throw 'Unsafe package cleanup target'}
    }
    Remove-Item -LiteralPath $stage,$unpacked -Recurse -Force
    Write-Host "PASS: downloaded/extracted Windows executable, four rehearsal modes, native window. $zip"
}
finally { Pop-Location }
