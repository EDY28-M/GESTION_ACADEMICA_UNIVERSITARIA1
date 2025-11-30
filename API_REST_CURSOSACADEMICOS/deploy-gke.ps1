# ========================================
# SCRIPT DE DESPLIEGUE AUTOMATIZADO
# PowerShell Script para GKE
# ========================================

param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectId,
    
    [string]$Region = "us-central1",
    [string]$Zone = "us-central1-a",
    [string]$ClusterName = "gke-cursos-academicos",
    [string]$RepoName = "api-cursos-repo"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " DESPLIEGUE EN GKE - API CURSOS ACADÉMICOS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Variables
$ImageName = "api-cursos-academicos"
$ImageTag = "latest"
$FullImagePath = "$Region-docker.pkg.dev/$ProjectId/$RepoName/${ImageName}:$ImageTag"

Write-Host "📋 Configuración:" -ForegroundColor Yellow
Write-Host "   Project ID: $ProjectId"
Write-Host "   Region: $Region"
Write-Host "   Zone: $Zone"
Write-Host "   Cluster: $ClusterName"
Write-Host "   Image: $FullImagePath"
Write-Host ""

# Paso 1: Configurar proyecto
Write-Host "🔐 Paso 1: Configurando proyecto de Google Cloud..." -ForegroundColor Green
gcloud config set project $ProjectId

# Paso 2: Habilitar APIs
Write-Host "🔧 Paso 2: Habilitando APIs necesarias..." -ForegroundColor Green
gcloud services enable container.googleapis.com
gcloud services enable artifactregistry.googleapis.com
gcloud services enable compute.googleapis.com

# Paso 3: Configurar Docker
Write-Host "🐳 Paso 3: Configurando autenticación Docker..." -ForegroundColor Green
gcloud auth configure-docker "$Region-docker.pkg.dev" --quiet

# Paso 4: Build Docker
Write-Host "🏗️ Paso 4: Construyendo imagen Docker..." -ForegroundColor Green
docker build -t "${ImageName}:$ImageTag" .

# Paso 5: Tag Docker
Write-Host "🏷️ Paso 5: Etiquetando imagen..." -ForegroundColor Green
docker tag "${ImageName}:$ImageTag" $FullImagePath

# Paso 6: Push Docker
Write-Host "📤 Paso 6: Subiendo imagen a Artifact Registry..." -ForegroundColor Green
docker push $FullImagePath

# Paso 7: Configurar kubectl
Write-Host "☸️ Paso 7: Configurando kubectl..." -ForegroundColor Green
gcloud container clusters get-credentials $ClusterName --zone $Zone --project $ProjectId

# Paso 8: Actualizar deployment.yaml con el Project ID correcto
Write-Host "📝 Paso 8: Actualizando deployment.yaml..." -ForegroundColor Green
$deploymentPath = "k8s/deployment.yaml"
$content = Get-Content $deploymentPath -Raw
$content = $content -replace '<TU-PROJECT-ID>', $ProjectId
$content | Set-Content $deploymentPath

# Paso 9: Aplicar configuraciones de Kubernetes
Write-Host "🚀 Paso 9: Desplegando en Kubernetes..." -ForegroundColor Green
kubectl apply -f k8s/secret.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml

# Paso 10: Esperar y mostrar IP
Write-Host ""
Write-Host "⏳ Esperando IP externa (puede tomar 2-3 minutos)..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

$attempts = 0
do {
    $service = kubectl get service api-cursos-academicos-lb -o jsonpath='{.status.loadBalancer.ingress[0].ip}'
    if ($service) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host " ✅ ¡DESPLIEGUE COMPLETADO!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "🌐 Tu API está disponible en:" -ForegroundColor Cyan
        Write-Host "   http://$service" -ForegroundColor White
        Write-Host "   http://$service/swagger" -ForegroundColor White
        Write-Host "   http://$service/health" -ForegroundColor White
        Write-Host ""
        break
    }
    $attempts++
    Write-Host "   Intento $attempts de 10... (IP aún pendiente)"
    Start-Sleep -Seconds 15
} while ($attempts -lt 10)

if (-not $service) {
    Write-Host ""
    Write-Host "⚠️ La IP aún no está lista. Ejecuta:" -ForegroundColor Yellow
    Write-Host "   kubectl get service api-cursos-academicos-lb" -ForegroundColor White
    Write-Host ""
}

Write-Host "📊 Estado actual:" -ForegroundColor Yellow
kubectl get pods
kubectl get services
