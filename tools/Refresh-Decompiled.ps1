# Regenerates the decompiled game source under <repo>/decompiled for easy grep/read reference.
# Run after every KSA game update:  pwsh ./tools/Refresh-Decompiled.ps1
param(
    [string]$GameDir = 'C:\Program Files\Ahwoo\Games\Kitten Space Agency',
    [string[]]$Assemblies = @('KSA', 'Brutal.ImGui', 'Brutal.ImGui.Abstractions', 'Brutal.ImGui.Extensions', 'Brutal.Numerics', 'Planet.Core')
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outRoot = Join-Path $repoRoot 'decompiled'

foreach ($name in $Assemblies) {
    if ($name -notmatch '^[A-Za-z0-9_.-]+$' -or $name -in @('.', '..')) {
        throw "Invalid assembly name: $name"
    }
    $dll = Join-Path $GameDir "$name.dll"
    if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) {
        throw "Assembly not found: $dll"
    }
}
$version = (Get-Item (Join-Path $GameDir 'KSA.dll')).VersionInfo.ProductVersion

if (-not (Get-Command ilspycmd -ErrorAction SilentlyContinue)) {
    Write-Host 'ilspycmd not found - installing as a global dotnet tool...'
    dotnet tool install -g ilspycmd
    if ($LASTEXITCODE -ne 0) { throw 'Failed to install ilspycmd' }
}

$versionFile = Join-Path $outRoot 'VERSION.txt'
if (Test-Path -LiteralPath $versionFile) { Remove-Item -LiteralPath $versionFile }

foreach ($name in $Assemblies) {
    $dll = Join-Path $GameDir "$name.dll"
    $outDir = Join-Path $outRoot $name
    if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
    New-Item -ItemType Directory -Path $outDir | Out-Null

    Write-Host "Decompiling $name.dll -> $outDir"
    ilspycmd -p -o $outDir -r $GameDir $dll
    if ($LASTEXITCODE -ne 0) { throw "ilspycmd failed for $name" }
}

Set-Content -Path $versionFile -Value "KSA $version decompiled $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
Write-Host "Done. Game version: $version"
