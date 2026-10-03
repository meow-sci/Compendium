# Regenerates the decompiled game source under <repo>/decompiled for easy grep/read reference.
# Run after every KSA game update:  pwsh ./tools/Refresh-Decompiled.ps1
param(
    [string]$GameDir = 'C:\Program Files\Kitten Space Agency',
    [string[]]$Assemblies = @('KSA', 'Brutal.ImGui', 'Brutal.Core.Numerics', 'Planet.Core')
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outRoot = Join-Path $repoRoot 'decompiled'

if (-not (Get-Command ilspycmd -ErrorAction SilentlyContinue)) {
    Write-Host 'ilspycmd not found - installing as a global dotnet tool...'
    dotnet tool install -g ilspycmd
}

foreach ($name in $Assemblies) {
    $dll = Join-Path $GameDir "$name.dll"
    if (-not (Test-Path $dll)) {
        Write-Warning "Skipping $name - not found at $dll"
        continue
    }

    $outDir = Join-Path $outRoot $name
    if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
    New-Item -ItemType Directory -Path $outDir | Out-Null

    Write-Host "Decompiling $name.dll -> $outDir"
    ilspycmd -p -o $outDir -r $GameDir $dll
    if ($LASTEXITCODE -ne 0) { throw "ilspycmd failed for $name" }
}

$version = (Get-Item (Join-Path $GameDir 'KSA.dll')).VersionInfo.ProductVersion
Set-Content -Path (Join-Path $outRoot 'VERSION.txt') -Value "KSA $version decompiled $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
Write-Host "Done. Game version: $version"
