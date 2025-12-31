# ========================================
# Generador de Clave Secreta JWT
# Este script genera una clave segura de 64 caracteres para JWT
# ========================================

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "   GENERADOR DE CLAVE SECRETA JWT" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

# Método 1: Base64 (Recomendado)
Write-Host "Generando clave Base64..." -ForegroundColor Yellow
try {
    $bytes = New-Object byte[] 48
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($bytes)
    $base64Key = [System.Convert]::ToBase64String($bytes)
    
    # Si es muy larga, tomar los primeros 64 caracteres
    if ($base64Key.Length -gt 64) {
        $base64Key = $base64Key.Substring(0, 64)
    }
    
    Write-Host ""
    Write-Host "Opción 1 (Base64 - RECOMENDADA):" -ForegroundColor Cyan
    Write-Host "────────────────────────────────────────────────────────────────" -ForegroundColor Gray
    Write-Host $base64Key -ForegroundColor White
    Write-Host "────────────────────────────────────────────────────────────────" -ForegroundColor Gray
    Write-Host ""
} catch {
    Write-Host "Error generando Base64: $_" -ForegroundColor Red
}

# Método 2: Alfanumérica con símbolos
Write-Host "Generando clave alfanumérica..." -ForegroundColor Yellow
$chars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*'
$key = -join ($chars.ToCharArray() | Get-Random -Count 64)

Write-Host ""
Write-Host "Opción 2 (Alfanumérica con símbolos):" -ForegroundColor Cyan
Write-Host "────────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host $key -ForegroundColor White
Write-Host "────────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "   INSTRUCCIONES" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "1. Copia cualquiera de las dos claves generadas arriba" -ForegroundColor Yellow
Write-Host "2. Ve a GitHub → Settings → Secrets and variables → Actions" -ForegroundColor Yellow
Write-Host "3. Crea un nuevo secret llamado: JWT_SECRET_KEY" -ForegroundColor Yellow
Write-Host "4. Pega la clave como valor del secret" -ForegroundColor Yellow
Write-Host ""
Write-Host "⚠️  IMPORTANTE: Guarda esta clave de forma segura. Si la pierdes," -ForegroundColor Red
Write-Host "   todos los tokens JWT existentes serán inválidos." -ForegroundColor Red
Write-Host ""
