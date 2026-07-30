param(
    [switch]$ValidateOnly
)

$ErrorActionPreference = "Stop"

function Get-NormalizedDirectoryPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not [System.IO.Path]::IsPathRooted($Path)) { throw "Directory path must be absolute: $Path" }
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $pathRoot = [System.IO.Path]::GetPathRoot($fullPath)
    if ($fullPath.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) -eq
        $pathRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)) {
        throw "Directory root is not allowed: $fullPath"
    }
    return $fullPath.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
}

function Test-IsStrictChildPath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Parent
    )

    $parentBoundary = $Parent + [System.IO.Path]::DirectorySeparatorChar
    return $Path.StartsWith($parentBoundary, [System.StringComparison]::OrdinalIgnoreCase)
}

$dev = Get-NormalizedDirectoryPath "D:\RimWorldModding\projects\yyyyy_GeneticDiversity"
$run = Get-NormalizedDirectoryPath "D:\steam\steamapps\common\RimWorld\Mods\yyyyy_GeneticDiversity"
$tempRoot = Get-NormalizedDirectoryPath "D:\RimWorldModding\projects"
$modsRoot = Get-NormalizedDirectoryPath "D:\steam\steamapps\common\RimWorld\Mods"
$log = Join-Path $dev "测试\phase3_runtime_sync_2026-07-28.log"
$allowedFiles = @("About\About.xml", "1.6\Assemblies\GeneticDiversity.dll")

if (-not (Test-IsStrictChildPath -Path $dev -Parent $tempRoot)) { throw "Invalid dev path: $dev" }
if (-not (Test-IsStrictChildPath -Path $run -Parent $modsRoot)) { throw "Invalid run path: $run" }
if ((Split-Path -Leaf $dev) -cne (Split-Path -Leaf $run)) { throw "Dev/run leaf names differ." }

$plannedFiles = foreach ($relativePath in $allowedFiles) {
    $source = [System.IO.Path]::GetFullPath((Join-Path $dev $relativePath))
    $destination = [System.IO.Path]::GetFullPath((Join-Path $run $relativePath))
    if (-not (Test-IsStrictChildPath -Path $source -Parent $dev)) { throw "Source escapes dev path: $source" }
    if (-not (Test-IsStrictChildPath -Path $destination -Parent $run)) { throw "Destination escapes run path: $destination" }
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Required source file is missing: $source" }
    [PSCustomObject]@{ RelativePath = $relativePath; Source = $source; Destination = $destination }
}

$existing = if (Test-Path -LiteralPath $run -PathType Container) {
    Get-ChildItem -LiteralPath $run -Recurse -File -Force | ForEach-Object { $_.FullName.Substring($run.Length + 1) }
} else {
    @()
}
$unexpected = $existing | Where-Object { $_ -notin $allowedFiles }
if ($unexpected) { throw "Runtime copy contains unexpected files: $($unexpected -join ', ')" }

if ($ValidateOnly) {
    Write-Output "Validation completed. No files were copied or deleted, and RimWorld was not queried or awaited."
    Write-Output "DEV=$dev"
    Write-Output "RUN=$run"
    $plannedFiles | ForEach-Object { Write-Output "WOULD_COPY=$($_.Source) -> $($_.Destination)" }
    return
}

"[$(Get-Date -Format o)] Waiting for RimWorldWin64 to exit. The process will not be terminated." | Set-Content -LiteralPath $log -Encoding UTF8
while (Get-Process -Name RimWorldWin64 -ErrorAction SilentlyContinue) {
    Start-Sleep -Seconds 5
}

New-Item -ItemType Directory -Force -Path (Join-Path $run "About"), (Join-Path $run "1.6\Assemblies") | Out-Null
foreach ($plannedFile in $plannedFiles) {
    Copy-Item -LiteralPath $plannedFile.Source -Destination $plannedFile.Destination -Force
}

$devDll = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $dev "1.6\Assemblies\GeneticDiversity.dll")).Hash
$runDll = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $run "1.6\Assemblies\GeneticDiversity.dll")).Hash
$devAbout = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $dev "About\About.xml")).Hash
$runAbout = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $run "About\About.xml")).Hash
if ($devDll -ne $runDll) { throw "DLL hashes differ after sync." }
if ($devAbout -ne $runAbout) { throw "About.xml hashes differ after sync." }

$runtimeFiles = Get-ChildItem -LiteralPath $run -Recurse -File -Force | ForEach-Object { $_.FullName.Substring($run.Length + 1) }
@(
    "[$(Get-Date -Format o)] Sync completed.",
    "DLL_SHA256=$devDll",
    "ABOUT_SHA256=$devAbout",
    "RUN_FILES=$($runtimeFiles -join '; ')"
) | Add-Content -LiteralPath $log -Encoding UTF8
Write-Output "Sync completed. DLL_SHA256=$devDll"
