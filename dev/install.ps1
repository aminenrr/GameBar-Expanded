<#
  Installs the Controls Hub dev build on this PC.
  1. Trusts the test certificate that signed this build (LocalMachine\TrustedPeople, needs admin).
  2. Installs the MSIX and its framework dependencies (VCLibs, .NET Native) with Add-AppxPackage.

  Run from the extracted package folder, in an elevated PowerShell (Run as administrator):
    powershell -ExecutionPolicy Bypass -File .\install.ps1
  Every CI build has its own certificate, so run this again for each new build.
#>
param([string]$Dir = $PSScriptRoot)
$ErrorActionPreference = 'Stop'

$admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $admin) {
    Write-Host 'This script needs admin rights to trust the test certificate.' -ForegroundColor Yellow
    Write-Host 'Open PowerShell with "Run as administrator" and run it again.'
    exit 1
}
if ($PSVersionTable.PSEdition -eq 'Core') { Import-Module Appx -UseWindowsPowerShell -WarningAction SilentlyContinue }

$cer = Get-ChildItem -Path $Dir -Filter *.cer | Select-Object -First 1
$pkg = Get-ChildItem -Path $Dir -File | Where-Object { $_.Extension -in '.msix', '.appx' } | Select-Object -First 1
if (-not $cer -or -not $pkg) { throw "Expected a .cer and a .msix next to this script in $Dir" }

Write-Host "Trusting $($cer.Name) (LocalMachine\TrustedPeople)"
Import-Certificate -FilePath $cer.FullName -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null

# Stop a running broker so the update can replace it.
Get-Process -Name ControlsHub.Broker -ErrorAction SilentlyContinue | Stop-Process -Force

$deps = @()
$depDir = Join-Path $Dir 'Dependencies\x64'
if (Test-Path $depDir) {
    $deps = Get-ChildItem -Path $depDir -File | Where-Object { $_.Extension -in '.msix', '.appx' } | ForEach-Object { $_.FullName }
}

Write-Host "Installing $($pkg.Name)"
if ($deps.Count -gt 0) {
    Add-AppxPackage -Path $pkg.FullName -DependencyPath $deps -ForceUpdateFromAnyVersion -ForceApplicationShutdown
} else {
    Add-AppxPackage -Path $pkg.FullName -ForceUpdateFromAnyVersion -ForceApplicationShutdown
}

$p = Get-AppxPackage -Name aminenrr.ControlsHub
Write-Host "Installed $($p.Name) $($p.Version)" -ForegroundColor Green
Write-Host 'Next: press Win+G, open the Widgets menu and pick "Controls".'
