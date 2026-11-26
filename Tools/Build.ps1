param([switch]$Run, [switch]$Test, [ValidateSet('Debug','Release')][string]$Configuration='Debug')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) { throw 'dotnet CLI not found. Install the .NET 10 SDK and Visual Studio 2026 .NET desktop development workload.' }
$sdkList = & dotnet --list-sdks
if ($LASTEXITCODE -ne 0 -or -not ($sdkList | Where-Object { $_ -match '^10\.0\.\d+\s' })) {
    throw ('Required .NET 10 SDK not detected. Installed SDKs: ' + ($sdkList -join '; '))
}
$logFolder = Join-Path $root 'BuildLogs'
New-Item -ItemType Directory -Force -Path $logFolder | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$log = Join-Path $logFolder ('build-' + $stamp + '.log')
Write-Host ('Building UDM10 on .NET 10, configuration: ' + $Configuration) -ForegroundColor Cyan
& dotnet build (Join-Path $root 'UDM10.sln') --configuration $Configuration --nologo 2>&1 | Tee-Object -FilePath $log
if ($LASTEXITCODE -ne 0) { throw ('dotnet build FAILED; inspect ' + $log) }
$bin = @{
    Client = Join-Path $root ('Code\Client\bin\' + $Configuration + '\net10.0-windows\Udm10.Client.exe')
    Server = Join-Path $root ('Code\Server\bin\' + $Configuration + '\net10.0-windows\Udm10.Server.exe')
    Tests  = Join-Path $root ('Code\Tests\bin\' + $Configuration + '\net10.0\Udm10.Tests.dll')
    Headless = Join-Path $root ('Code\Headless\bin\' + $Configuration + '\net10.0\Udm10.Headless.dll')
}
foreach ($key in $bin.Keys) {
    if (-not (Test-Path $bin[$key])) { throw ('Build reported success but required output is missing: ' + $bin[$key]) }
}
[ordered]@{
    BuiltAt = (Get-Date).ToString('o')
    Framework = 'net10.0 / net10.0-windows'
    SDK = (& dotnet --version)
    Configuration = $Configuration
    Machine = $env:COMPUTERNAME
    Client = $bin.Client
    Server = $bin.Server
    Result = 'BUILD_SUCCEEDED'
} | ConvertTo-Json | Set-Content (Join-Path $logFolder 'build-status.json') -Encoding UTF8
Write-Host 'Successfully built all six .NET 10 projects.' -ForegroundColor Green
if ($Test) {
    $testOutput = Join-Path $root ('Extra\TestResults\' + $stamp)
    New-Item -ItemType Directory -Force -Path $testOutput | Out-Null
    & dotnet $bin.Tests $testOutput
    $testExit = $LASTEXITCODE
    $json = Join-Path $testOutput 'results.json'
    if (Test-Path $json) {
        try { & (Join-Path $PSScriptRoot 'Import-TestResults.ps1') -ResultJson $json }
        catch { Write-Warning ('Tests generated output, but workbook import failed. Close Excel and re-run the importer. ' + $_.Exception.Message) }
    }
    if ($testExit -ne 0) { throw ('At least one automated test FAILED; inspect ' + $testOutput) }
    Write-Host ('Tests completed. Real results saved in ' + $testOutput) -ForegroundColor Green
}
if ($Run) {
    Start-Process -FilePath $bin.Server -WorkingDirectory (Split-Path $bin.Server -Parent)
    Start-Process -FilePath $bin.Client -WorkingDirectory (Split-Path $bin.Client -Parent)
}
