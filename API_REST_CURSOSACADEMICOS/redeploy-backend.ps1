# ========================================
# SCRIPT: DESTRUIR Y REDESPLEGAR BACKEND
# ========================================

param(
    [Parameter(Mandatory=$false)]
    [string]$ProjectId = "flash-adapter-424617-u4",
    
    [string]$Region = "us-central1",
    [string]$Zone = "us-central1-a",
    [string]$ClusterName = "gke-cursos-academicos",
    [string]$RepoName = "api-cursos-repo"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " DESTRUIR Y REDESPLEGAR BACKEND" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Variables
$ImageName = "api-cursos-academicos"
$ImageTag = "latest"
$FullImagePath = "$Region-docker.pkg.dev/$ProjectId/$RepoName/${ImageName}:$ImageTag"

Write-Host "Configuracion:" -ForegroundColor Yellow
Write-Host "   Project ID: $ProjectId"
Write-Host "   Region: $Region"
Write-Host "   Zone: $Zone"
Write-Host "   Cluster: $ClusterName"
Write-Host "   Image: $FullImagePath"
Write-Host ""

# ========================================
# FASE 1: DESTRUIR RECURSOS EXISTENTES
# ========================================
Write-Host "========================================" -ForegroundColor Red
Write-Host " FASE 1: DESTRUYENDO RECURSOS" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Red
Write-Host ""

# Configurar kubectl
Write-Host "Configurando kubectl..." -ForegroundColor Yellow
try {
    gcloud container clusters get-credentials $ClusterName --zone $Zone --project $ProjectId 2>&1 | Out-Null
    Write-Host "kubectl configurado" -ForegroundColor Green
} catch {
    Write-Host "No se pudo conectar al cluster. Puede que no exista o este inactivo." -ForegroundColor Yellow
    Write-Host "Continuando con el despliegue..." -ForegroundColor Yellow
}

# Verificar si hay recursos
Write-Host "Verificando recursos existentes..." -ForegroundColor Yellow
$existingDeployment = kubectl get deployment api-cursos-academicos -o jsonpath='{.metadata.name}' 2>&1

if ($existingDeployment -and $existingDeployment -ne "" -and $existingDeployment -notmatch "error") {
    Write-Host "Recursos encontrados. Eliminando..." -ForegroundColor Yellow
    
    # Eliminar recursos
    kubectl delete deployment api-cursos-academicos --ignore-not-found=true 2>&1 | Out-Null
    kubectl delete service api-cursos-academicos-lb --ignore-not-found=true 2>&1 | Out-Null
    kubectl delete secret api-secrets --ignore-not-found=true 2>&1 | Out-Null
    kubectl delete configmap api-config --ignore-not-found=true 2>&1 | Out-Null
    
    Write-Host "Esperando a que se eliminen los recursos..." -ForegroundColor Yellow
    Start-Sleep -Seconds 15
    
    Write-Host "Recursos eliminados" -ForegroundColor Green
} else {
    Write-Host "No hay recursos existentes para eliminar" -ForegroundColor Cyan
}

Write-Host ""

# ========================================
# FASE 2: CONSTRUIR Y SUBIR IMAGEN
# ========================================
Write-Host "========================================" -ForegroundColor Green
Write-Host " FASE 2: CONSTRUYENDO Y SUBIENDO IMAGEN" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

# Configurar proyecto
Write-Host "Configurando proyecto de Google Cloud..." -ForegroundColor Yellow
gcloud config set project $ProjectId 2>&1 | Out-Null

# Configurar Docker
Write-Host "Configurando autenticacion Docker..." -ForegroundColor Yellow
gcloud auth configure-docker "$Region-docker.pkg.dev" --quiet 2>&1 | Out-Null

# Build Docker
Write-Host "Construyendo imagen Docker..." -ForegroundColor Yellow
docker build -t "${ImageName}:$ImageTag" .
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error al construir la imagen Docker" -ForegroundColor Red
    exit 1
}
Write-Host "Imagen construida" -ForegroundColor Green

# Tag Docker
Write-Host "Etiquetando imagen..." -ForegroundColor Yellow
docker tag "${ImageName}:$ImageTag" $FullImagePath

# Push Docker
Write-Host "Subiendo imagen a Artifact Registry..." -ForegroundColor Yellow
docker push $FullImagePath
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error al subir la imagen" -ForegroundColor Red
    exit 1
}
Write-Host "Imagen subida exitosamente" -ForegroundColor Green

Write-Host ""

# ========================================
# FASE 3: DESPLEGAR EN KUBERNETES
# ========================================
Write-Host "========================================" -ForegroundColor Green
Write-Host " FASE 3: DESPLEGANDO EN KUBERNETES" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

# Configurar kubectl
Write-Host "Configurando kubectl..." -ForegroundColor Yellow
gcloud container clusters get-credentials $ClusterName --zone $Zone --project $ProjectId 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error al configurar kubectl. Verifica que el cluster exista." -ForegroundColor Red
    exit 1
}

# Actualizar deployment.yaml con el Project ID correcto
Write-Host "Actualizando deployment.yaml..." -ForegroundColor Yellow
$deploymentPath = "k8s/deployment.yaml"
if (Test-Path $deploymentPath) {
    $content = Get-Content $deploymentPath -Raw
    $content = $content -replace 'us-central1-docker\.pkg\.dev/[^/]+/', "us-central1-docker.pkg.dev/$ProjectId/"
    $content | Set-Content $deploymentPath
    Write-Host "deployment.yaml actualizado" -ForegroundColor Green
}

# Aplicar configuraciones de Kubernetes
Write-Host "Desplegando en Kubernetes..." -ForegroundColor Yellow

Write-Host "   Aplicando secrets..." -ForegroundColor Cyan
kubectl apply -f k8s/secret.yaml 2>&1 | Out-Null

Write-Host "   Aplicando configmap..." -ForegroundColor Cyan
kubectl apply -f k8s/configmap.yaml 2>&1 | Out-Null

Write-Host "   Aplicando deployment..." -ForegroundColor Cyan
kubectl apply -f k8s/deployment.yaml 2>&1 | Out-Null

Write-Host "   Aplicando service..." -ForegroundColor Cyan
kubectl apply -f k8s/service.yaml 2>&1 | Out-Null

Write-Host "Recursos aplicados" -ForegroundColor Green

# Esperar a que los pods esten listos
Write-Host ""
Write-Host "Esperando a que los pods esten listos..." -ForegroundColor Yellow
$maxAttempts = 30
$attempt = 0
$ready = $false

while ($attempt -lt $maxAttempts -and -not $ready) {
    Start-Sleep -Seconds 5
    $podStatus = kubectl get pods -l app=api-cursos-academicos -o jsonpath='{.items[0].status.phase}' 2>&1
    if ($podStatus -eq "Running") {
        $ready = $true
        Write-Host "Pods estan corriendo" -ForegroundColor Green
    } else {
        $attempt++
        Write-Host "   Intento $attempt/$maxAttempts... (Estado: $podStatus)" -ForegroundColor Cyan
    }
}

if (-not $ready) {
    Write-Host "Los pods aun no estan listos. Revisa los logs:" -ForegroundColor Yellow
    Write-Host "   kubectl logs -l app=api-cursos-academicos" -ForegroundColor White
}

# ========================================
# FASE 4: OBTENER IP EXTERNA
# ========================================
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " FASE 4: OBTENIENDO IP EXTERNA" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

Write-Host "Esperando IP externa (puede tomar 2-3 minutos)..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

$attempts = 0
$service = $null
do {
    $service = kubectl get service api-cursos-academicos-lb -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>&1
    if ($service -and $service -notmatch "error" -and $service.Length -gt 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host " DESPLIEGUE COMPLETADO!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Tu API esta disponible en:" -ForegroundColor Cyan
        Write-Host "   http://$service" -ForegroundColor White
        Write-Host "   http://$service/swagger" -ForegroundColor White
        Write-Host "   http://$service/health" -ForegroundColor White
        Write-Host ""
        break
    }
    $attempts++
    Write-Host "   Intento $attempts de 10... (IP aun pendiente)" -ForegroundColor Cyan
    Start-Sleep -Seconds 15
} while ($attempts -lt 10)

if (-not $service -or $service -match "error" -or $service.Length -eq 0) {
    Write-Host ""
    Write-Host "La IP aun no esta lista. Ejecuta:" -ForegroundColor Yellow
    Write-Host "   kubectl get service api-cursos-academicos-lb" -ForegroundColor White
    Write-Host ""
}

# Mostrar estado final
Write-Host "Estado actual:" -ForegroundColor Yellow
kubectl get pods
Write-Host ""
kubectl get services

Write-Host ""
Write-Host "Proceso completado!" -ForegroundColor Green
