param([Parameter(Mandatory=$true)][string]$CourseCode,[Parameter(Mandatory=$true)][string]$GroupCode)
$ErrorActionPreference='Stop'
foreach($value in @($CourseCode,$GroupCode)) { if($value -notmatch '^[A-Za-z0-9_-]+$') { throw 'CourseCode and GroupCode may contain only letters, digits, underscores and hyphens.' } }
$root=Split-Path $PSScriptRoot -Parent
$seven=Get-Command 7z.exe -ErrorAction SilentlyContinue
if(-not $seven) {
    $candidate=Join-Path $env:ProgramFiles '7-Zip\7z.exe'
    if(Test-Path $candidate) { $seven=Get-Item $candidate }
}
if(-not $seven) { throw '7-Zip was not found. Install 7-Zip from its official website, then run this script again. No ZIP is mislabeled as 7z.' }
$exe=if($seven.Path){$seven.Path}else{$seven.FullName}
$destination=Join-Path (Split-Path $root -Parent) ($CourseCode+'-'+$GroupCode+'-UDM_10.7z')
if(Test-Path $destination) { throw ('Archive already exists. Rename it before packaging again: '+$destination) }
Push-Location $root
try {
    & $exe a '-t7z' $destination '.\*' '-xr!Build' '-xr!bin' '-xr!obj' '-xr!.git' '-xr!.vs' '-xr!*.user' '-xr!*.suo' '-xr!*.pdb' '-xr!*.part' '-xr!*.tmp' '-xr!*.zip' '-xr!*.7z'
    if($LASTEXITCODE -ne 0) { throw 'Archive creation failed.' }
    & $exe t $destination
    if($LASTEXITCODE -ne 0) { throw 'Archive verification failed.' }
    Write-Host ('Created '+$destination) -ForegroundColor Green
    Write-Host 'Review names, student IDs, real test results, GitHub/video links and school report formatting before submitting.'
} finally { Pop-Location }
