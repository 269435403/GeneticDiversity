param(
    [switch]$ValidateOnly
)

$ErrorActionPreference = "Stop"

function Get-NormalizedDirectoryPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not [System.IO.Path]::IsPathRooted($Path)) { throw "Directory path must be absolute: $Path" }
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $pathRoot = [System.IO.Path]::GetPathRoot($fullPath)
    $trimmedPath = $fullPath.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    $trimmedRoot = $pathRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    if ($trimmedPath -eq $trimmedRoot) { throw "Directory root is not allowed: $fullPath" }
    return $trimmedPath
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
$log = Join-Path $dev "测试\phase2_runtime_sync_2026-07-28.log"
$deadline = [DateTime]::Now.AddHours(12)
$allowedFiles = @("About\About.xml", "1.6\Assemblies\GeneticDiversity.dll")

try {
    if (-not (Test-IsStrictChildPath -Path $dev -Parent $tempRoot)) { throw "Invalid dev path: $dev" }
    if (-not (Test-IsStrictChildPath -Path $run -Parent $modsRoot)) { throw "Invalid run path: $run" }
    if ((Split-Path -Leaf $dev) -cne (Split-Path -Leaf $run)) { throw "Dev/run leaf names differ." }

    $plannedFiles = foreach ($relativePath in $allowedFiles) {
        if ([System.IO.Path]::IsPathRooted($relativePath)) { throw "Whitelist entry must be relative: $relativePath" }
        $source = [System.IO.Path]::GetFullPath((Join-Path $dev $relativePath))
        $destination = [System.IO.Path]::GetFullPath((Join-Path $run $relativePath))
        if (-not (Test-IsStrictChildPath -Path $source -Parent $dev)) { throw "Source escapes dev path: $source" }
        if (-not (Test-IsStrictChildPath -Path $destination -Parent $run)) { throw "Destination escapes run path: $destination" }
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Required source file is missing: $source" }
        [PSCustomObject]@{ RelativePath = $relativePath.Replace('/', '\'); Source = $source; Destination = $destination }
    }

    $plannedRelativePaths = @($plannedFiles | ForEach-Object { $_.RelativePath } | Sort-Object -Unique)
    $normalizedAllowedFiles = @($allowedFiles | ForEach-Object { $_.Replace('/', '\') } | Sort-Object -Unique)
    if ($plannedRelativePaths.Count -ne $allowedFiles.Count -or (Compare-Object -ReferenceObject $normalizedAllowedFiles -DifferenceObject $plannedRelativePaths)) {
        throw "Synchronization plan does not exactly match the runtime whitelist."
    }

    $existingFiles = if (Test-Path -LiteralPath $run -PathType Container) {
        @(Get-ChildItem -LiteralPath $run -Recurse -File -Force | ForEach-Object { $_.FullName.Substring($run.Length + 1).Replace('/', '\') })
    } else { @() }
    $unexpectedFiles = @($existingFiles | Where-Object { $_ -notin $normalizedAllowedFiles })
    if ($unexpectedFiles.Count -gt 0) { throw "Runtime whitelist mismatch: $($unexpectedFiles -join ', ')" }

    if ($ValidateOnly) {
        Write-Output "Validation completed. No files were copied or deleted, no log was written, and RimWorld was not queried or awaited."
        Write-Output "DEV=$dev"
        Write-Output "RUN=$run"
        $plannedFiles | ForEach-Object { Write-Output "WOULD_COPY=$($_.Source) -> $($_.Destination)" }
        return
    }

    "[$([DateTime]::Now.ToString('s'))] Waiting for RimWorldWin64 to exit before DLL synchronization." | Set-Content -LiteralPath $log -Encoding UTF8
    while (Get-Process -Name RimWorldWin64 -ErrorAction SilentlyContinue) {
        if ([DateTime]::Now -ge $deadline) { throw "Timed out after 12 hours while waiting for RimWorldWin64 to exit." }
        Start-Sleep -Seconds 5
    }

    New-Item -ItemType Directory -Path (Join-Path $run "About") -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $run "1.6\Assemblies") -Force | Out-Null
    foreach ($plannedFile in $plannedFiles) { Copy-Item -LiteralPath $plannedFile.Source -Destination $plannedFile.Destination -Force }

    $devHash = (Get-FileHash -LiteralPath (Join-Path $dev "1.6\Assemblies\GeneticDiversity.dll") -Algorithm SHA256).Hash
    $runHash = (Get-FileHash -LiteralPath (Join-Path $run "1.6\Assemblies\GeneticDiversity.dll") -Algorithm SHA256).Hash
    if ($devHash -ne $runHash) { throw "DLL hash mismatch after synchronization: dev=$devHash run=$runHash" }

    $runtimeFiles = @(Get-ChildItem -LiteralPath $run -Recurse -File -Force | ForEach-Object { $_.FullName.Substring($run.Length + 1).Replace('\', '/') } | Sort-Object)
    $expectedRuntimeFiles = @($normalizedAllowedFiles | ForEach-Object { $_.Replace('\', '/') } | Sort-Object)
    if (Compare-Object -ReferenceObject $expectedRuntimeFiles -DifferenceObject $runtimeFiles) { throw "Runtime whitelist mismatch: $($runtimeFiles -join ', ')" }

    "[$([DateTime]::Now.ToString('s'))] SUCCESS dev/run DLL SHA256=$devHash; runtime files=$($runtimeFiles -join ', ')" | Add-Content -LiteralPath $log -Encoding UTF8
}
catch {
    if (-not $ValidateOnly) { "[$([DateTime]::Now.ToString('s'))] FAILED $($_.Exception.Message)" | Add-Content -LiteralPath $log -Encoding UTF8 }
    Write-Error $_
    exit 1
}
