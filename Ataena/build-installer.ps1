# Script para generar el instalador de Ataena con Inno Setup
# Requisitos: Inno Setup 6 instalado (https://jrsoftware.org/isdl.php)
# Uso: .\build-installer.ps1

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

Write-Host "=== Build Instalador Ataena ===" -ForegroundColor Cyan

# 1. Publicar la aplicación
Write-Host "`n[1/2] Publicando aplicación..." -ForegroundColor Yellow
dotnet publish -c Release -r win-x64 --self-contained true -o .\publish
if ($LASTEXITCODE -ne 0) { exit 1 }

# 2. Compilar con Inno Setup
$isccPaths = @(
    "${env:LOCALAPPDATA}\Programs\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
)

$iscc = $null
foreach ($path in $isccPaths) {
    if (Test-Path $path) {
        $iscc = $path
        break
    }
}

if (-not $iscc) {
    Write-Host "`n[ERROR] Inno Setup no encontrado." -ForegroundColor Red
    Write-Host "Descarga e instala desde: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Write-Host "`nLa carpeta publish/ ya está generada. Puedes compilar manualmente:" -ForegroundColor Gray
    Write-Host "  Abre Installer\Ataena.iss en Inno Setup y pulsa Compile (Ctrl+F9)" -ForegroundColor Gray
    exit 1
}

Write-Host "`n[2/2] Compilando instalador con Inno Setup..." -ForegroundColor Yellow
& $iscc /Qp ".\Installer\Ataena.iss"
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host "`n=== Listo ===" -ForegroundColor Green
Write-Host "Instalador generado en: .\Releases\Ataena-Setup-1.0.5.exe" -ForegroundColor Cyan
