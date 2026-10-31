param([string]$OutputRoot = 'artifacts/secret-audit')
$ErrorActionPreference = 'Stop'
$version = '8.30.1'
$expectedSha256 = 'd29144deff3a68aa93ced33dddf84b7fdc26070add4aa0f4513094c8332afc4e'
$output = [IO.Path]::GetFullPath($OutputRoot)
New-Item -ItemType Directory -Path $output -Force | Out-Null
$archive = Join-Path $output "gitleaks_${version}_windows_x64.zip"
$tool = Join-Path $output 'gitleaks'
if (-not (Test-Path -LiteralPath $archive)) {
    Invoke-WebRequest "https://github.com/gitleaks/gitleaks/releases/download/v$version/gitleaks_${version}_windows_x64.zip" -OutFile $archive
}
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedSha256) { throw 'Gitleaks checksum mismatch' }
Expand-Archive -LiteralPath $archive -DestinationPath $tool -Force
& (Join-Path $tool 'gitleaks.exe') git . --log-opts='--all --full-history' --redact=100 --no-banner --report-format json --report-path (Join-Path $output 'history-redacted.json')
if ($LASTEXITCODE -ne 0) { throw 'Secret audit failed. Inspect only the redacted local report; do not publish secret values.' }
