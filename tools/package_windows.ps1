param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$GodotExecutable = "C:\Users\Surface\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe",
    [switch]$SkipExport
)

$ErrorActionPreference = "Stop"
$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
$exportDirectory = Join-Path $ProjectRoot "build\Wick-Windows"
$archivePath = Join-Path $ProjectRoot "build\Wick-Windows.zip"
$assembliesDirectory = Join-Path $exportDirectory "data_Wick_windows_x86_64"

if (-not $SkipExport) {
    if (-not (Test-Path -LiteralPath $GodotExecutable -PathType Leaf)) {
        throw "Godot Mono executable not found: $GodotExecutable"
    }

    & $GodotExecutable --headless --path $ProjectRoot --export-release "Windows Desktop"
    if ($LASTEXITCODE -ne 0) {
        throw "Godot Windows export failed with exit code $LASTEXITCODE."
    }
}

$requiredFiles = @(
    (Join-Path $exportDirectory "Wick.exe"),
    (Join-Path $exportDirectory "Wick.pck"),
    (Join-Path $exportDirectory "README.txt"),
    (Join-Path $assembliesDirectory "Wick.dll")
)

foreach ($requiredFile in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "Required Windows package file is missing: $requiredFile"
    }
}

if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    $exportDirectory,
    $archivePath,
    [System.IO.Compression.CompressionLevel]::Optimal,
    $false)

$archive = [System.IO.Compression.ZipFile]::OpenRead($archivePath)
try {
    $entries = @($archive.Entries | ForEach-Object { $_.FullName.Replace("\", "/") })
    $requiredEntries = @(
        "Wick.exe",
        "Wick.pck",
        "README.txt",
        "data_Wick_windows_x86_64/Wick.dll"
    )

    foreach ($requiredEntry in $requiredEntries) {
        if ($entries -notcontains $requiredEntry) {
            throw "Required Windows package entry is missing: $requiredEntry"
        }
    }
}
finally {
    $archive.Dispose()
}

Write-Output "Created verified Windows package: $archivePath"
