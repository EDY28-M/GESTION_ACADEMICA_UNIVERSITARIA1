# ========================================
# Script de Verificacion de Configuracion GCP
# Verifica que todos los secrets esten configurados en GitHub
# ========================================

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "VERIFICACION DE CONFIGURACION GCP/GITHUB" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Colores para output
$ErrorColor = "Red"
$SuccessColor = "Green"
$WarningColor = "Yellow"
$InfoColor = "Cyan"

# ========================================
# PASO 1: Verificar que gcloud este instalado
# ========================================
Write-Host "[PASO 1] Verificando Google Cloud SDK..." -ForegroundColor $InfoColor
try {
    $gcloudVersion = gcloud --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Google Cloud SDK esta instalado" -ForegroundColor $SuccessColor
        Write-Host "   Version: $($gcloudVersion[0])" -ForegroundColor Gray
    } else {
        Write-Host "❌ Google Cloud SDK no esta instalado" -ForegroundColor $ErrorColor
        Write-Host "   Instala desde: https://cloud.google.com/sdk/docs/install" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "❌ Error al verificar gcloud: $_" -ForegroundColor $ErrorColor
    exit 1
}

Write-Host ""

# ========================================
# PASO 2: Verificar autenticación
# ========================================
Write-Host "[PASO 2] Verificando autenticación en Google Cloud..." -ForegroundColor $InfoColor
try {
    $currentAccount = gcloud auth list --filter=status:ACTIVE --format="value(account)" 2>&1
    if ($LASTEXITCODE -eq 0 -and $currentAccount) {
        Write-Host "✅ Autenticado como: $currentAccount" -ForegroundColor $SuccessColor
    } else {
        Write-Host "⚠️  No hay cuenta autenticada activa" -ForegroundColor $WarningColor
        Write-Host "   Ejecuta: gcloud auth login" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Error al verificar autenticacion: $_" -ForegroundColor $ErrorColor
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
        Write-Host "⚠️  No se pudieron listar los proyectos" -ForegroundColor $WarningColor
    }
} catch {
    Write-Host "❌ Error al listar proyectos: $_" -ForegroundColor $ErrorColor
}

Write-Host ""

# ========================================
# PASO 4: Verificar Service Account
# ========================================
Write-Host "[PASO 4] Verificando Service Account para GitHub Actions..." -ForegroundColor $InfoColor
Write-Host "   Buscando: github-actions-deployer@..." -ForegroundColor Gray

$projectId = Read-Host "Ingresa tu GCP_PROJECT_ID (ej: flash-adapter-424617-u4)"

if ([string]::IsNullOrWhiteSpace($projectId)) {
    Write-Host "❌ Project ID no puede estar vacio" -ForegroundColor $ErrorColor
    exit 1
}

try {
    gcloud config set project $projectId 2>&1 | Out-Null
    $serviceAccounts = gcloud iam service-accounts list --format="table(email)" 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Service Accounts en el proyecto:" -ForegroundColor Gray
        Write-Host $serviceAccounts
        Write-Host ""
        Write-Host "✅ Si ves 'github-actions-deployer@...' esta configurado" -ForegroundColor $SuccessColor
        Write-Host "⚠️  Si no lo ves, necesitas crearlo (consulta GITHUB_SECRETS_GUIDE.md)" -ForegroundColor $WarningColor
    } else {
        Write-Host "⚠️  No se pudieron listar los service accounts" -ForegroundColor $WarningColor
    }
} catch {
    Write-Host "❌ Error al listar service accounts: $_" -ForegroundColor $ErrorColor
}

Write-Host ""

# ========================================
# PASO 5: Instrucciones para GitHub Secrets
# ========================================
Write-Host "[PASO 5] Configuracion de GitHub Secrets" -ForegroundColor $InfoColor
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Ve a tu repositorio en GitHub:" -ForegroundColor Yellow
Write-Host "  1. Settings → Secrets and variables → Actions" -ForegroundColor White
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
    Write-Host "     - Genera una clave secreta (minimo 32 caracteres)" -ForegroundColor Gray
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
Write-Host "¿Quieres generar una JWT_SECRET_KEY ahora? (S/N)" -ForegroundColor Yellow
$generateKey = Read-Host

if ($generateKey -eq "S" -or $generateKey -eq "s") {
    Write-Host ""
    Write-Host "Generando JWT_SECRET_KEY..." -ForegroundColor $InfoColor
    $bytes = New-Object byte[] 64
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    $jwtKey = [System.Convert]::ToBase64String($bytes)
    Write-Host ""
    Write-Host "✅ Tu JWT_SECRET_KEY (copiala y guardala en GitHub Secrets):" -ForegroundColor $SuccessColor
    Write-Host $jwtKey -ForegroundColor White
    Write-Host ""
    Write-Host "⚠️  IMPORTANTE: Guarda esta clave de forma segura" -ForegroundColor $WarningColor
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
Write-Host "Para mas informacion, consulta: GITHUB_SECRETS_GUIDE.md" -ForegroundColor Gray
