param([string]$Dotnet = '')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (!$Dotnet) {
    $localSdk = Join-Path $PSScriptRoot 'dotnet\dotnet.exe'
    if (Test-Path -LiteralPath $localSdk) { $Dotnet = $localSdk }
    else { $Dotnet = (Get-Command dotnet -ErrorAction Stop).Source }
}
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot 'cli'
$env:NUGET_PACKAGES = Join-Path $PSScriptRoot 'packages'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
& $Dotnet publish (Join-Path $projectRoot 'src\IPAClipboard.csproj') -c Release -r win-x64 --self-contained true -o (Join-Path $projectRoot 'release') "-p:RestoreConfigFile=$projectRoot\NuGet.Config" -p:NuGetAudit=false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -p:DebugType=none
if ($LASTEXITCODE -ne 0) { throw '发布失败' }
$exe = Join-Path $projectRoot 'release\IPA音标助手.exe'
if (!(Test-Path -LiteralPath $exe)) { throw '发布后没有找到EXE' }
Get-Item -LiteralPath $exe | Select-Object FullName, Length
Get-FileHash -LiteralPath $exe -Algorithm SHA256
