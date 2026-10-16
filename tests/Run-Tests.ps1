param([string]$Executable = '')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$exe = if ($Executable) { $Executable } else { Join-Path $projectRoot 'release\IPA音标助手.exe' }
$output = Join-Path $PSScriptRoot ('run-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $output -Force | Out-Null
# Prove that launch does not discover the project SDK or a machine-installed .NET runtime.
$oldRoot = $env:DOTNET_ROOT
$oldPath = $env:PATH
try {
    $env:DOTNET_ROOT = Join-Path $output 'nonexistent-runtime'
    $env:PATH = "$env:SystemRoot\System32;$env:SystemRoot"
    $process = Start-Process -FilePath $exe -ArgumentList @('--self-test', ('"' + $output + '"')) -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0) { throw "自测失败，查看 $output\FAILED.txt" }
} finally { $env:DOTNET_ROOT = $oldRoot; $env:PATH = $oldPath }
$results = Get-Content -LiteralPath (Join-Path $output 'results.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if (!$results.success) { throw '自测结果失败' }
$persistent = Get-Clipboard -Raw
if ($persistent -ne "θðʃʒŋɜːaɪ̃") { throw '退出后剪贴板内容不匹配' }
$afterExit = @{ success = $true; text = $persistent; codepoints = @($persistent.ToCharArray() | ForEach-Object { 'U+{0:X4}' -f [int]$_ }); description = '子进程退出后，父进程实际读取系统剪贴板' }
$afterExit | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $output 'clipboard-after-exit.json') -Encoding UTF8
Write-Output "通过 $($results.count) 项检查，退出后剪贴板验证通过。报告：$output"
