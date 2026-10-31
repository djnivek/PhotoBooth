param(
    [Parameter(Mandatory)][ValidatePattern('^[^<>\r\n]+\\[^<>\r\n]+$')][string]$Account,
    [ValidatePattern('^[A-Za-z]:\\[^<>\r\n]+\.exe$')][string]$ApplicationPath = 'C:\PhotoBooth\ColorsParty.exe',
    [ValidateSet('Windows10','Windows11')][string]$TargetWindows = 'Windows10',
    [string]$OutputPath = 'artifacts/kiosk/AssignedAccess.xml'
)
$ErrorActionPreference = 'Stop'
# Generate a reviewable file only. Never change accounts, registry, policy or this PC's shell.
$escape = { param($value) [System.Security.SecurityElement]::Escape($value) }
$accountXml = & $escape $Account
$applicationXml = & $escape $ApplicationPath
$profile = '{F09CD4E6-C587-45A6-9520-5AF59F29EA83}'
$start = if ($TargetWindows -eq 'Windows11') {
    '<v5:StartPins><![CDATA[{"pinnedList":[]}]]></v5:StartPins>'
} else {
    @'
<StartLayout><![CDATA[<LayoutModificationTemplate xmlns="http://schemas.microsoft.com/Start/2014/LayoutModification" xmlns:defaultlayout="http://schemas.microsoft.com/Start/2014/FullDefaultLayout" xmlns:start="http://schemas.microsoft.com/Start/2014/StartLayout" Version="1"><LayoutOptions StartTileGroupCellWidth="6"/><DefaultLayoutOverride><StartLayoutCollection><defaultlayout:StartLayout GroupCellWidth="6"/></StartLayoutCollection></DefaultLayoutOverride></LayoutModificationTemplate>]]></StartLayout>
'@
}
$configuration = @"
<?xml version="1.0" encoding="utf-8"?>
<AssignedAccessConfiguration xmlns="http://schemas.microsoft.com/AssignedAccess/2017/config" xmlns:rs5="http://schemas.microsoft.com/AssignedAccess/201810/config" xmlns:v5="http://schemas.microsoft.com/AssignedAccess/2022/config">
  <Profiles>
    <Profile Id="$profile" Name="Colors Party">
      <AllAppsList><AllowedApps><App DesktopAppPath="$applicationXml" rs5:AutoLaunch="true" rs5:AutoLaunchArguments="--kiosk"/></AllowedApps></AllAppsList>
      $start
      <Taskbar ShowTaskbar="false"/>
    </Profile>
  </Profiles>
  <Configs><Config><Account>$accountXml</Account><DefaultProfile Id="$profile"/></Config></Configs>
</AssignedAccessConfiguration>
"@
[xml]$configuration | Out-Null
$output = [IO.Path]::GetFullPath($OutputPath)
New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($output)) -Force | Out-Null
[IO.File]::WriteAllText($output,$configuration,[Text.UTF8Encoding]::new($false))
Write-Output "Configuration generated only: $output"
Write-Output 'No Windows policy was applied. Read docs/KIOSQUE_WINDOWS.md before using this on the booth.'
