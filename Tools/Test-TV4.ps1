param([string]$DotnetPath = 'dotnet')

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$localSdk = Join-Path (Split-Path $repoRoot -Parent) '.tools\dotnet\dotnet.exe'
if ($DotnetPath -eq 'dotnet' -and (Test-Path -LiteralPath $localSdk)) { $DotnetPath = $localSdk }
$evidenceRoot = Join-Path $repoRoot ('BuildLogs\TV4-refactor-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
$offlineConfig = Join-Path $evidenceRoot 'NuGet.Config'
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content -LiteralPath $offlineConfig
Push-Location $repoRoot
try {
    & $DotnetPath --info | Out-File (Join-Path $evidenceRoot 'environment.txt')
    if ($LASTEXITCODE -ne 0) { throw 'Cannot run .NET SDK.' }
    git rev-parse HEAD | Out-File (Join-Path $evidenceRoot 'base-commit.txt')
    Get-FileHash -Algorithm SHA256 -LiteralPath @(
        (Join-Path $repoRoot 'Code\ServerCore\ServerEngine.cs'),
        (Join-Path $repoRoot 'Code\Tests\Program.cs'),
        (Join-Path $repoRoot 'Code\Tests\TV4ReceiveChecks.cs')
    ) | Format-List | Out-File (Join-Path $evidenceRoot 'source-hashes.txt')
    & $DotnetPath build (Join-Path $repoRoot 'UDM10.sln') --configfile $offlineConfig -m:1 -nr:false -p:UseSharedCompilation=false 2>&1 |
        Tee-Object -FilePath (Join-Path $evidenceRoot 'build.txt')
    if ($LASTEXITCODE -ne 0) { throw 'Integrated solution build failed.' }
    # The official harness launches Headless with dotnet from PATH in TC_36.
    $previousPath = $env:PATH
    try {
        $resolvedDotnet = (Get-Command $DotnetPath -ErrorAction Stop).Source
        $env:PATH = (Split-Path $resolvedDotnet -Parent) + [IO.Path]::PathSeparator + $env:PATH
        $testsDll = Join-Path $repoRoot 'Code\Tests\bin\Debug\net10.0\Udm10.Tests.dll'
        & $DotnetPath $testsDll (Join-Path $evidenceRoot 'official') 2>&1 |
            Tee-Object -FilePath (Join-Path $evidenceRoot 'official-console.txt')
        $officialExit = $LASTEXITCODE
        & $DotnetPath $testsDll --tv4 (Join-Path $evidenceRoot 'tv4') 2>&1 |
            Tee-Object -FilePath (Join-Path $evidenceRoot 'tv4-console.txt')
        $tv4Exit = $LASTEXITCODE
        if ($officialExit -ne 0 -or $tv4Exit -ne 0) { throw 'One or more tests failed; retain all evidence.' }
    } finally { $env:PATH = $previousPath }
    Write-Host ('Verified refactor branch. Evidence: ' + $evidenceRoot)
} finally { Pop-Location }
