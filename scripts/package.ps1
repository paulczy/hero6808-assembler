param(
    [string]$Configuration = "Release",
    [string[]]$RuntimeIdentifiers = @("win-x64", "osx-x64"),
    [string]$OutputRoot = "artifacts"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$projectPath = Join-Path $repoRoot "src/Hero6808.Cli/Hero6808.Cli.csproj"
$publishRoot = Join-Path $repoRoot (Join-Path $OutputRoot "publish")
$packageRoot = Join-Path $repoRoot (Join-Path $OutputRoot "packages")

New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null

Add-Type -AssemblyName System.IO.Compression.FileSystem

foreach ($rid in $RuntimeIdentifiers) {
    $publishDir = Join-Path $publishRoot $rid
    if (Test-Path $publishDir) {
        Remove-Item -Path $publishDir -Recurse -Force
    }

    dotnet publish $projectPath `
        -c $Configuration `
        -r $rid `
        --self-contained true `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:DebugSymbols=false `
        /p:DebugType=None `
        -o $publishDir

    # Ensure release archives do not include debug symbol files.
    Get-ChildItem -Path $publishDir -Filter *.pdb -Recurse -File -ErrorAction SilentlyContinue |
        Remove-Item -Force -ErrorAction SilentlyContinue

    $zipName = "Hero6808-$rid.zip"
    $zipPath = Join-Path $packageRoot $zipName
    if (Test-Path $zipPath) {
        Remove-Item -Path $zipPath -Force
    }

    [System.IO.Compression.ZipFile]::CreateFromDirectory($publishDir, $zipPath)
    Write-Host "Created $zipPath"
}

