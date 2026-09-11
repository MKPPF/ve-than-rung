param(
    [string]$BuildDirectory = 'Builds/GraduationDemo_FontFixed',
    [string]$OutputZip = 'Releases/VeThanRung-Windows-x64.zip'
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not [IO.Path]::IsPathRooted($BuildDirectory)) { $BuildDirectory = Join-Path $projectRoot $BuildDirectory }
if (-not [IO.Path]::IsPathRooted($OutputZip)) { $OutputZip = Join-Path $projectRoot $OutputZip }
$buildRoot = (Resolve-Path -LiteralPath $BuildDirectory).Path
$zipPath = [IO.Path]::GetFullPath($OutputZip)
foreach ($required in @('VeThanRung.exe', 'UnityPlayer.dll', 'VeThanRung_Data/globalgamemanagers', 'VeThanRung_Data/boot.config')) {
    if (-not (Test-Path -LiteralPath (Join-Path $buildRoot $required) -PathType Leaf)) { throw "Incomplete build: $required" }
}
if (Test-Path -LiteralPath $zipPath) { throw "Archive already exists. Choose a new -OutputZip: $zipPath" }
if ($zipPath.StartsWith($buildRoot.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Output ZIP must be outside the build folder.' }
New-Item -ItemType Directory -Path (Split-Path -Parent $zipPath) -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::Open($zipPath, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem -LiteralPath $buildRoot -File -Recurse) {
        $relative = $file.FullName.Substring($buildRoot.Length).TrimStart('\', '/').Replace('\', '/')
        if ($relative -match '(^|/)[^/]*DoNotShip(/|$)|\.(pdb|mdb|log|tmp)$') { continue }
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, "VeThanRung/$relative", [IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
    $extras = @{
        'Docs/PLAY-WINDOWS.txt' = 'VeThanRung/READ-ME-FIRST.txt'
        'Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt' = 'VeThanRung/Licenses/LiberationSans-OFL.txt'
        'Assets/ThirdParty/UnityPhysicsExamples2D/LICENSE.md' = 'VeThanRung/Licenses/UnityPhysicsExamples2D-LICENSE.md'
        'Assets/ThirdParty/UnityPhysicsExamples2D/NOTICE.md' = 'VeThanRung/Licenses/UnityPhysicsExamples2D-NOTICE.md'
        'THIRD_PARTY_NOTICES.md' = 'VeThanRung/Licenses/THIRD_PARTY_NOTICES.md'
    }
    foreach ($source in $extras.Keys) {
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path $projectRoot $source), $extras[$source]) | Out-Null
    }
} finally { $archive.Dispose() }
$verify = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    foreach ($name in @('VeThanRung/VeThanRung.exe', 'VeThanRung/UnityPlayer.dll', 'VeThanRung/VeThanRung_Data/globalgamemanagers', 'VeThanRung/READ-ME-FIRST.txt')) {
        if ($null -eq $verify.GetEntry($name)) { throw "Missing archive entry: $name" }
    }
} finally { $verify.Dispose() }
$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText($zipPath + '.sha256', "$hash  $([IO.Path]::GetFileName($zipPath))`n")
Write-Output "Verified archive: $zipPath"
Write-Output "SHA256: $hash"
