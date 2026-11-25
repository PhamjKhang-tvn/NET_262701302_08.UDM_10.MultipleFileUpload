param([string]$Folder)
$ErrorActionPreference='Stop'
if (-not $Folder) { $Folder=Join-Path ([Environment]::GetFolderPath('MyDocuments')) ('UDM10_Demo_'+(Get-Date -Format 'yyyyMMdd-HHmmss')) }
New-Item -ItemType Directory -Force -Path $Folder | Out-Null
[IO.File]::WriteAllText((Join-Path $Folder '01_readme.txt'),'UDM10 synthetic demo data. No personal information.',[Text.Encoding]::UTF8)
[IO.File]::WriteAllBytes((Join-Path $Folder '02_empty.bin'),[byte[]]@())
$buffer=New-Object byte[] 65536
$rng=[Security.Cryptography.RandomNumberGenerator]::Create()
try {
    foreach ($spec in @(@('03_small.bin',16),@('04_large.bin',512))) {
        $path=Join-Path $Folder $spec[0]
        $stream=[IO.File]::Open($path,[IO.FileMode]::CreateNew)
        try { for($i=0;$i -lt [int]$spec[1];$i++) { $rng.GetBytes($buffer); $stream.Write($buffer,0,$buffer.Length) } }
        finally { $stream.Dispose() }
    }
}
finally { $rng.Dispose() }
Get-ChildItem $Folder -File | ForEach-Object { Get-FileHash $_.FullName -Algorithm SHA256 } | Export-Csv (Join-Path $Folder 'sha256.csv') -NoTypeInformation -Encoding UTF8
Write-Host ('Created demo data: '+$Folder) -ForegroundColor Green
Start-Process explorer.exe -ArgumentList ('"'+$Folder+'"')
