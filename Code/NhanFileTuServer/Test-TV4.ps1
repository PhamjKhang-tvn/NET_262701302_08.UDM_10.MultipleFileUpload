param([string]$DotnetPath = 'dotnet')

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$bundledSdk = Join-Path (Split-Path $repoRoot -Parent) '.tools\dotnet\dotnet.exe'
if ($DotnetPath -eq 'dotnet' -and (Test-Path -LiteralPath $bundledSdk)) {
    $DotnetPath = $bundledSdk
}
$evidenceRoot = Join-Path $repoRoot ('BuildLogs\TV4-build-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
# No external packages are needed for these projects.
$offlineConfig = Join-Path $evidenceRoot 'NuGet.Config'
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content -LiteralPath $offlineConfig
Push-Location $repoRoot
try {
    & $DotnetPath --info | Out-File (Join-Path $evidenceRoot 'environment.txt')
    if ($LASTEXITCODE -ne 0) { throw 'Cannot run .NET SDK.' }
    git rev-parse HEAD | Out-File (Join-Path $evidenceRoot 'base-commit.txt')
    Get-FileHash -Algorithm SHA256 -LiteralPath @(
        (Join-Path $repoRoot 'Code\NhanFileTuServer\ServerEngine.cs'),
        (Join-Path $repoRoot 'Code\TV1\Code\Tests\Program.cs')
    ) | Format-List | Out-File (Join-Path $evidenceRoot 'source-hashes.txt')
    foreach ($project in @('Headless\Udm10.Headless.csproj', 'Tests\Udm10.Tests.csproj')) {
        & $DotnetPath build (Join-Path 'Code\TV1\Code' $project) --configfile $offlineConfig -m:1 -nr:false -p:UseSharedCompilation=false 2>&1 |
            Tee-Object -FilePath (Join-Path $evidenceRoot 'build.txt') -Append
        if ($LASTEXITCODE -ne 0) { throw ('Build failed: ' + $project) }
    }
    & $DotnetPath 'Code\TV1\Code\Tests\bin\Debug\net10.0\Udm10.Tests.dll' (Join-Path $repoRoot 'BuildLogs\TV4')
    if ($LASTEXITCODE -ne 0) { throw 'TV4 tests failed; keep the results.txt and logs for diagnosis.' }
    Write-Host ('Build evidence: ' + $evidenceRoot)
} finally {
    Pop-Location
}
