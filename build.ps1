param(
    [string]$Configuration = "Release",
    [string]$RimWorldDir = "D:\steam\steamapps\common\RimWorld"
)

$ErrorActionPreference = "Stop"
$project = Join-Path $PSScriptRoot "Source\GeneticDiversity\GeneticDiversity.csproj"
if (-not (Test-Path -LiteralPath $project)) {
    throw "Project not found: $project"
}

$harmonyCandidates = @(
    (Join-Path $RimWorldDir "..\..\workshop\content\294100\2009463077\Current\Assemblies\0Harmony.dll"),
    (Join-Path $RimWorldDir "Mods\Harmony\Current\Assemblies\0Harmony.dll")
)
$harmonyPath = $harmonyCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $harmonyPath) {
    throw "0Harmony.dll was not found. Install Harmony or pass a valid RimWorldDir."
}

$env:DOTNET_CLI_UI_LANGUAGE = "en-US"
dotnet build $project -c $Configuration -p:RimWorldDir="$RimWorldDir" -p:HarmonyPath="$harmonyPath"
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE."
}

