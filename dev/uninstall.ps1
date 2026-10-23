<#
  Removes the Controls Hub dev build.
    powershell -ExecutionPolicy Bypass -File .\uninstall.ps1              removes the app
    powershell -ExecutionPolicy Bypass -File .\uninstall.ps1 -RemoveCerts also removes the CN=aminenrr
                                                                          test certificates (needs admin)
#>
param([switch]$RemoveCerts)
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSEdition -eq 'Core') { Import-Module Appx -UseWindowsPowerShell -WarningAction SilentlyContinue }

Get-Process -Name ControlsHub.Broker -ErrorAction SilentlyContinue | Stop-Process -Force

$p = Get-AppxPackage -Name aminenrr.ControlsHub
if ($p) {
    Remove-AppxPackage -Package $p.PackageFullName
    Write-Host "Removed $($p.PackageFullName)"
} else {
    Write-Host 'Controls Hub is not installed.'
}

if ($RemoveCerts) {
    $admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $admin) { Write-Host 'Removing certificates needs admin rights.' -ForegroundColor Yellow; exit 1 }
    Get-ChildItem Cert:\LocalMachine\TrustedPeople | Where-Object { $_.Subject -eq 'CN=aminenrr' } | ForEach-Object {
        Write-Host "Removing certificate $($_.Thumbprint)"
        Remove-Item $_.PSPath
    }
}
