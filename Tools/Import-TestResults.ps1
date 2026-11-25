param([Parameter(Mandatory=$true)][string]$ResultJson, [string]$Workbook)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (-not $Workbook) { $Workbook = Join-Path $root 'Extra\TestCases\UDM10_Test_Cases.xlsx' }
$ResultJson = (Resolve-Path $ResultJson).Path
$data = Get-Content $ResultJson -Raw -Encoding UTF8 | ConvertFrom-Json
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$backup = Join-Path (Split-Path $ResultJson -Parent) 'test-cases-before-import.xlsx'
Copy-Item $Workbook $backup -Force
$temp = $Workbook + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
Copy-Item $Workbook $temp
$ns = 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'
$zip = [IO.Compression.ZipFile]::Open($temp,[IO.Compression.ZipArchiveMode]::Update)
function Read-XmlEntry([string]$path) {
    $entry = $zip.GetEntry($path)
    if (-not $entry) { throw ('Missing workbook entry: ' + $path) }
    $reader = New-Object IO.StreamReader($entry.Open())
    try { $xml = New-Object Xml.XmlDocument; $xml.PreserveWhitespace=$true; $xml.LoadXml($reader.ReadToEnd()); return ,$xml }
    finally { $reader.Dispose() }
}
function Save-XmlEntry([string]$path,[xml]$xml) {
    $old=$zip.GetEntry($path); if ($old) { $old.Delete() }
    $entry=$zip.CreateEntry($path,[IO.Compression.CompressionLevel]::Optimal)
    $stream=$entry.Open()
    try { $settings=New-Object Xml.XmlWriterSettings; $settings.Encoding=New-Object Text.UTF8Encoding($false); $writer=[Xml.XmlWriter]::Create($stream,$settings); $xml.Save($writer); $writer.Dispose() }
    finally { $stream.Dispose() }
}
function Cell-Text([xml]$xml,[string]$address) {
    $manager=New-Object Xml.XmlNamespaceManager($xml.NameTable); $manager.AddNamespace('x',$ns)
    $cell=$xml.SelectSingleNode('//x:c[@r="'+$address+'"]',$manager)
    if ($cell) { return $cell.InnerText }; return ''
}
function Set-Text([xml]$xml,[string]$address,[string]$value) {
    $manager=New-Object Xml.XmlNamespaceManager($xml.NameTable); $manager.AddNamespace('x',$ns)
    $cell=$xml.SelectSingleNode('//x:c[@r="'+$address+'"]',$manager)
    if (-not $cell) {
        $rowNumber=[regex]::Match($address,'[0-9]+').Value
        $sheetData=$xml.SelectSingleNode('//x:sheetData',$manager)
        $row=$xml.SelectSingleNode('//x:row[@r="'+$rowNumber+'"]',$manager)
        if (-not $row) { $row=$xml.CreateElement('row',$ns); $row.SetAttribute('r',$rowNumber); [void]$sheetData.AppendChild($row) }
        $cell=$xml.CreateElement('c',$ns); $cell.SetAttribute('r',$address); [void]$row.AppendChild($cell)
    }
    # Preserve the style index; replace only the value/formula payload.
    while ($cell.HasChildNodes) { [void]$cell.RemoveChild($cell.FirstChild) }
    $cell.SetAttribute('t','inlineStr')
    $inline=$xml.CreateElement('is',$ns); $text=$xml.CreateElement('t',$ns); $text.InnerText=$value
    [void]$inline.AppendChild($text); [void]$cell.AppendChild($inline)
}
try {
    $workbookXml=Read-XmlEntry 'xl/workbook.xml'; $relations=Read-XmlEntry 'xl/_rels/workbook.xml.rels'
    $manager=New-Object Xml.XmlNamespaceManager($workbookXml.NameTable); $manager.AddNamespace('x',$ns)
    $paths=@{}
    foreach ($sheet in $workbookXml.SelectNodes('//x:sheet',$manager)) {
        $rid=$sheet.GetAttribute('id','http://schemas.openxmlformats.org/officeDocument/2006/relationships')
        $rel=$relations.DocumentElement.ChildNodes | Where-Object { $_.Id -eq $rid } | Select-Object -First 1
        $target=$rel.Target
        if ($target.StartsWith('/')) { $target=$target.TrimStart('/') } else { $target='xl/'+$target }
        $paths[$sheet.name]=$target
    }
    $firstSheet=$workbookXml.SelectSingleNode('//x:sheet',$manager).name
    $mainPath=$paths[$firstSheet]; $main=Read-XmlEntry $mainPath
    foreach ($result in $data.Records) {
        if ($result.Id -notmatch '^TC_([0-9]{2})$') { continue }
        $number=[int]$Matches[1]; $row=$number+2
        if ((Cell-Text $main ('B'+$row)) -ne $result.Id) { throw ('Test ID does not match workbook row: '+$result.Id) }
        $column='G'
        if (Cell-Text $main ('G'+$row)) { $column='H' }
        if (Cell-Text $main ('H'+$row)) { $column='I' }
        $actual=$data.StartedUtc+' | '+$result.Stage+' | '+[Math]::Round($result.Seconds,3)+' s | '+$result.Actual
        Set-Text $main ($column+$row) $actual
        Set-Text $main ('J'+$row) $result.Stage
        if ($paths.ContainsKey($result.Id)) {
            $proof=Read-XmlEntry $paths[$result.Id]
            Set-Text $proof 'B7' ($data.StartedUtc+' | '+$data.Environment)
            Set-Text $proof 'B9' $actual
            Set-Text $proof 'B11' ('Evidence folder: '+(Split-Path $ResultJson -Parent))
            Save-XmlEntry $paths[$result.Id] $proof
        }
    }
    Save-XmlEntry $mainPath $main
    $zip.Dispose(); $zip=$null
    Copy-Item $temp $Workbook -Force
    Write-Host ('Imported actual results into '+$Workbook) -ForegroundColor Green
}
finally { if ($zip) { $zip.Dispose() }; if (Test-Path $temp) { Remove-Item $temp -Force } }
