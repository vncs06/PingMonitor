param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Resolve paths relative to this script's location
$ScriptDir = $PSScriptRoot
$RootDir = Split-Path -Parent $ScriptDir
$ProjectPath = Join-Path $RootDir 'PingMonitor.csproj'
$MsiDir = Join-Path $ScriptDir 'Msi'
$BundleDir = Join-Path $ScriptDir 'Bundle'

# Ensure script runs from its own directory for relative operations
Set-Location $ScriptDir

Write-Host "Publishing app ($Configuration, $Runtime)..."
dotnet publish $ProjectPath -c $Configuration -r $Runtime --self-contained $true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

$pubDir = Join-Path $RootDir "bin\$Configuration\net8.0-windows\$Runtime\publish"
Write-Host "Using publish dir: $pubDir"

# Clean previous payload to keep MSI deterministic
if (Test-Path (Join-Path $MsiDir 'Payload')) { Remove-Item (Join-Path $MsiDir 'Payload') -Recurse -Force }
New-Item -ItemType Directory -Path (Join-Path $MsiDir 'Payload') | Out-Null

Copy-Item (Join-Path $pubDir 'PingMonitor.exe') $MsiDir -Force
Copy-Item (Join-Path $pubDir '*') (Join-Path $MsiDir 'Payload') -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Building MSI..."
$wix = ${env:WIX}
if (-not $wix) {
  $candleCmd = Get-Command candle.exe -ErrorAction SilentlyContinue
  if ($candleCmd) { $wix = Split-Path -Parent $candleCmd.Path }
}
if (-not $wix) {
  $candidates = @(
    'C:\\Program Files (x86)\\WiX Toolset v3.14\\bin',
    'C:\\Program Files (x86)\\WiX Toolset v3.11\\bin',
    'C:\\Program Files\\WiX Toolset v3.14\\bin',
    'C:\\Program Files\\WiX Toolset v3.11\\bin'
  )
  foreach ($p in $candidates) { if (Test-Path $p) { $wix = $p; break } }
}
if (-not $wix) {
  Write-Host "WiX não encontrado no sistema. Baixando WiX 3.14 (binaries)..."
  $toolsDir = Join-Path $ScriptDir 'tools'
  $wixDir = Join-Path $toolsDir 'wix314'
  $wixBin = Join-Path $wixDir 'bin'
  if (-not (Test-Path $wixBin)) {
    New-Item -ItemType Directory -Path $wixBin -Force | Out-Null
    $zipUrl = 'https://github.com/wixtoolset/wix3/releases/download/wix314rtm/wix314-binaries.zip'
    $zipPath = Join-Path $toolsDir 'wix314-binaries.zip'
    Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath -UseBasicParsing
    Expand-Archive -Path $zipPath -DestinationPath $wixDir -Force
    Remove-Item $zipPath -Force
  }
  $candle = Get-ChildItem -Path $wixDir -Recurse -Filter 'candle.exe' -ErrorAction SilentlyContinue | Select-Object -First 1
  if (-not $candle) { throw 'Falha ao preparar WiX 3.14 binaries.' }
  $wix = $candle.DirectoryName
}
$env:Path = "$wix;" + $env:Path

Push-Location $MsiDir
if (-not (Test-Path .\Payload)) { New-Item -ItemType Directory -Path .\Payload | Out-Null }

"Compiling WiX sources..." | Write-Host
candle.exe -ext WixUtilExtension -out obj\ Product.wxs AppFiles.wxs
light.exe -ext WixUIExtension -ext WixUtilExtension -out PingMonitor.msi obj\Product.wixobj obj\AppFiles.wixobj
Pop-Location

Write-Host "Building Bundle..."
Push-Location $BundleDir
candle.exe -ext WixBalExtension -ext WixUtilExtension -out obj\ Bundle.wxs
light.exe -ext WixBalExtension -ext WixUtilExtension -out PingMonitorSetup.exe obj\Bundle.wixobj
Pop-Location

# Decide outputs and cleanup
$bundlePath = Join-Path $BundleDir 'PingMonitorSetup.exe'
$msiPath = Join-Path $MsiDir 'PingMonitor.msi'

Write-Host "Done. Output:"
if (Test-Path $bundlePath) {
  $artifacts = Join-Path $RootDir 'artifacts'
  if (-not (Test-Path $artifacts)) { New-Item -ItemType Directory -Path $artifacts | Out-Null }
  $ridSafe = ($Runtime -replace "[^a-zA-Z0-9-]", "")
  $stamp = Get-Date -Format 'yyyyMMdd-HHmm'
  $outBundle = Join-Path $artifacts ("PingMonitorSetup-" + $ridSafe + ".exe")
  Copy-Item $bundlePath $outBundle -Force
  Write-Host "Bundle copiado para: $outBundle"
  # Keep only the Bundle as distributable; MSI is an internal artifact
  try {
    if (Test-Path $msiPath) { Remove-Item $msiPath -Force }
    $payloadPath = Join-Path $MsiDir 'Payload'
    if (Test-Path $payloadPath) { Remove-Item $payloadPath -Recurse -Force }
    $bundleObj = Join-Path $BundleDir 'obj'
    if (Test-Path $bundleObj) { Remove-Item $bundleObj -Recurse -Force }
  } catch { Write-Warning $_ }
} else {
  Write-Warning "Bundle não foi gerado: $bundlePath não existe. Verifique erros do candle/light acima."
  if (Test-Path $msiPath) {
    Write-Host "MSI gerado: $(Resolve-Path $msiPath)"
  } else {
    Write-Host "MSI não gerado."
  }
}
