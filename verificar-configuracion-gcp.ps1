# ========================================
# Script de VerificaciÃ³n de ConfiguraciÃ³n GCP
# Verifica que todos los secrets estÃ©n configurados en GitHub
# ========================================

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "VERIFICACIÃ“N DE CONFIGURACIÃ“N GCP/GITHUB" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Colores para output
$ErrorColor = "Red"
$SuccessColor = "Green"
$WarningColor = "Yellow"
$InfoColor = "Cyan"

# ========================================
# PASO 1: Verificar que gcloud estÃ© instalado
# ========================================
Write-Host "[PASO 1] Verificando Google Cloud SDK..." -ForegroundColor $InfoColor
try {
    $gcloudVersion = gcloud --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "âœ… Google Cloud SDK estÃ¡ instalado" -ForegroundColor $SuccessColor
        Write-Host "   VersiÃ³n: $($gcloudVersion[0])" -ForegroundColor Gray
    } else {
        Write-Host "âŒ Google Cloud SDK no estÃ¡ instalado" -ForegroundColor $ErrorColor
        Write-Host "   Instala desde: https://cloud.google.com/sdk/docs/install" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "âŒ Error al verificar gcloud: $_" -ForegroundColor $ErrorColor
    exit 1
}

Write-Host ""

# ========================================
# PASO 2: Verificar autenticaciÃ³n
# ========================================
Write-Host "[PASO 2] Verificando autenticaciÃ³n en Google Cloud..." -ForegroundColor $InfoColor
try {
    $currentAccount = gcloud auth list --filter=status:ACTIVE --format="value(account)" 2>&1
    if ($LASTEXITCODE -eq 0 -and $currentAccount) {
        Write-Host "âœ… Autenticado como: $currentAccount" -ForegroundColor $SuccessColor
    } else {
        Write-Host "âš ï¸  No hay cuenta autenticada activa" -ForegroundColor $WarningColor
        Write-Host "   Ejecuta: gcloud auth login" -ForegroundColor Yellow
    }
} catch {
    Write-Host "âŒ Error al verificar autenticaciÃ³n: $_" -ForegroundColor $ErrorColor
}

Write-Host ""

# ========================================
# PASO 3: Listar proyectos disponibles
# ========================================
Write-Host "[PASO 3] Listando proyectos disponibles..." -ForegroundColor $InfoColor
try {
    $projects = gcloud projects list --format="table(projectId,name)" 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Proyectos disponibles:" -ForegroundColor Gray
        Write-Host $projects
    } else {
        Write-Host "âš ï¸  No se pudieron listar los proyectos" -ForegroundColor $WarningColor
    }
} catch {
    Write-Host "âŒ Error al listar proyectos: $_" -ForegroundColor $ErrorColor
}

Write-Host ""

# ========================================
# PASO 4: Verificar Service Account
# ========================================
Write-Host "[PASO 4] Verificando Service Account para GitHub Actions..." -ForegroundColor $InfoColor
Write-Host "   Buscando: github-actions-deployer@..." -ForegroundColor Gray

$projectId = Read-Host "Ingresa tu GCP_PROJECT_ID (ej: flash-adapter-424617-u4)"

if ([string]::IsNullOrWhiteSpace($projectId)) {
    Write-Host "âŒ Project ID no puede estar vacÃ­o" -ForegroundColor $ErrorColor
    exit 1
}

try {
    gcloud config set project $projectId 2>&1 | Out-Null
    $serviceAccounts = gcloud iam service-accounts list --format="table(email)" 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Service Accounts en el proyecto:" -ForegroundColor Gray
        Write-Host $serviceAccounts
        Write-Host ""
        Write-Host "âœ… Si ves 'github-actions-deployer@...' estÃ¡ configurado" -ForegroundColor $SuccessColor
        Write-Host "âš ï¸  Si no lo ves, necesitas crearlo (consulta GITHUB_SECRETS_GUIDE.md)" -ForegroundColor $WarningColor
    } else {
        Write-Host "âš ï¸  No se pudieron listar los service accounts" -ForegroundColor $WarningColor
    }
} catch {
    Write-Host "âŒ Error al listar service accounts: $_" -ForegroundColor $ErrorColor
}

Write-Host ""

# ========================================
# PASO 5: Instrucciones para GitHub Secrets
# ========================================
Write-Host "[PASO 5] ConfiguraciÃ³n de GitHub Secrets" -ForegroundColor $InfoColor
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Ve a tu repositorio en GitHub:" -ForegroundColor Yellow
Write-Host "  1. Settings â†’ Secrets and variables â†’ Actions" -ForegroundColor White
Write-Host "  2. Haz clic en 'New repository secret'" -ForegroundColor White
Write-Host ""
Write-Host "Secrets requeridos:" -ForegroundColor Yellow
Write-Host "  1. GCP_SA_KEY" -ForegroundColor White
Write-Host "     - Descarga el JSON del Service Account creado" -ForegroundColor Gray
Write-Host "     - Copia TODO el contenido del archivo JSON" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. GCP_PROJECT_ID" -ForegroundColor White
Write-Host "     - Valor: $projectId" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. CLOUD_RUN_CONNECTION_STRING" -ForegroundColor White
Write-Host "     - Connection string de tu base de datos SQL Server" -ForegroundColor Gray
Write-Host ""
Write-Host "  4. JWT_SECRET_KEY" -ForegroundColor White
Write-Host "     - Genera una clave secreta (mÃ­nimo 32 caracteres)" -ForegroundColor Gray
Write-Host "     - Puedes usar: .\generate-jwt-key.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "  5. JWT_ISSUER" -ForegroundColor White
Write-Host "     - Valor sugerido: GestionAcademicaAPI" -ForegroundColor Gray
Write-Host ""
Write-Host "  6. JWT_AUDIENCE" -ForegroundColor White
Write-Host "     - Valor sugerido: GestionAcademicaClients" -ForegroundColor Gray
Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Â¿Quieres generar una JWT_SECRET_KEY ahora? (S/N)" -ForegroundColor Yellow
$generateKey = Read-Host

if ($generateKey -eq "S" -or $generateKey -eq "s") {
    Write-Host ""
    Write-Host "Generando JWT_SECRET_KEY..." -ForegroundColor $InfoColor
    $bytes = New-Object byte[] 64
    $rng = New-Object System.Security.Cryptography.RNGCryptoServiceProvider
    $rng.GetBytes($bytes)
    $rng.Dispose()
    $jwtKey = [System.Convert]::ToBase64String($bytes)
    Write-Host ""
    Write-Host "âœ… Tu JWT_SECRET_KEY (cÃ³piala y guÃ¡rdala en GitHub Secrets):" -ForegroundColor $SuccessColor
    Write-Host $jwtKey -ForegroundColor White
    Write-Host ""
    Write-Host "âš ï¸  IMPORTANTE: Guarda esta clave de forma segura" -ForegroundColor $WarningColor
    Write-Host "   No la compartas ni la subas al repositorio" -ForegroundColor $WarningColor
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "VERIFICACION COMPLETADA" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Proximos pasos:" -ForegroundColor Yellow
Write-Host "  1. Configura todos los secrets en GitHub" -ForegroundColor White
Write-Host "  2. Haz push a la rama main/master" -ForegroundColor White
Write-Host "  3. El workflow de GitHub Actions se ejecutara automaticamente" -ForegroundColor White
Write-Host ""
Write-Host "Consulta GITHUB_SECRETS_GUIDE.md para mas informacion" -ForegroundColor Gray
