# Cleans build outputs, keeping only Installer artifacts folder
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$paths = @(
  Join-Path $root 'bin',
  Join-Path $root 'obj',
  Join-Path $root 'Installer\Msi\obj',
  Join-Path $root 'Installer\Bundle\obj',
  Join-Path $root 'Installer\Msi\Payload'
)
foreach ($p in $paths) {
  if (Test-Path $p) { Remove-Item $p -Recurse -Force -ErrorAction SilentlyContinue }
}
Write-Host 'Clean complete.'
