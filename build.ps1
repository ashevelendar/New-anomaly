$ErrorActionPreference = 'Stop'

$modRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceRoot = Join-Path $modRoot 'Source\SightstealerColony'
$rimWorldRoot = Resolve-Path (Join-Path $modRoot '..\..')
$managedRoot = Join-Path $rimWorldRoot 'RimWorldWin64_Data\Managed'
$harmonyRoot = Join-Path $rimWorldRoot 'Mods\HarmonyMod\Current\Assemblies'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outputPath = Join-Path $modRoot '1.6\Assemblies\SightstealerColony.dll'
$assemblyReference = Join-Path $managedRoot 'Assembly-CSharp.dll'
$unityReference = Join-Path $managedRoot 'UnityEngine.CoreModule.dll'
$mathematicsReference = Join-Path $managedRoot 'Unity.Mathematics.dll'
$harmonyReference = Join-Path $harmonyRoot '0Harmony.dll'
$sourceFiles = Get-ChildItem -LiteralPath $sourceRoot -Filter '*.cs' -File | Select-Object -ExpandProperty FullName

foreach ($requiredPath in @($compiler, $assemblyReference, $unityReference, $mathematicsReference, $harmonyReference)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required build file was not found: $requiredPath"
    }
}

if (-not (Test-Path -LiteralPath (Split-Path -Parent $outputPath))) {
    New-Item -ItemType Directory -Path (Split-Path -Parent $outputPath) -Force | Out-Null
}

& $compiler /nologo /target:library /optimize+ /platform:x64 /out:$outputPath `
  /reference:$assemblyReference `
  /reference:$unityReference `
  /reference:$mathematicsReference `
  /reference:$harmonyReference `
  $sourceFiles

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Built $outputPath"
