# ========================================
# 🚀 GUÍA DE DESPLIEGUE EN GKE
# API REST Cursos Académicos
# ========================================

## 📋 REQUISITOS PREVIOS

### 1. Instalar Herramientas

| Herramienta | URL de Descarga |
|-------------|-----------------|
| Google Cloud CLI | https://cloud.google.com/sdk/docs/install |
| Terraform | https://developer.hashicorp.com/terraform/downloads |
| Docker Desktop | https://www.docker.com/products/docker-desktop |

### 2. Verificar Instalaciones (ejecuta en PowerShell)
```powershell
gcloud --version
terraform --version
docker --version
```

---

## 🔐 PASO 1: Configurar Google Cloud

### 1.1 Iniciar sesión en Google Cloud
```powershell
gcloud auth login
```
> Se abrirá el navegador para autenticarte con tu cuenta de Google.

### 1.2 Crear o seleccionar proyecto
```powershell
# Ver proyectos existentes
gcloud projects list

# Crear nuevo proyecto (opcional)
gcloud projects create mi-proyecto-api-cursos --name="API Cursos Académicos"

# Seleccionar proyecto
gcloud config set project TU-PROJECT-ID
```

### 1.3 Habilitar APIs necesarias
```powershell
gcloud services enable container.googleapis.com
gcloud services enable artifactregistry.googleapis.com
gcloud services enable compute.googleapis.com
```

### 1.4 Configurar autenticación para Docker
```powershell
gcloud auth configure-docker us-central1-docker.pkg.dev
```

### 1.5 Instalar kubectl (si no lo tienes)
```powershell
gcloud components install kubectl
```

---

## 🏗️ PASO 2: Crear Infraestructura con Terraform

### 2.1 Navegar a la carpeta terraform
```powershell
cd "c:\Users\cater\Downloads\DOCKER CONTENEDOR\BACKEND_DEVELOMENT\BACKEND_DEVELOMENT\API_REST_CURSOSACADEMICOS\terraform"
```

### 2.2 Crear archivo de variables
```powershell
# Copia el archivo de ejemplo
Copy-Item terraform.tfvars.example terraform.tfvars

# Edita el archivo y reemplaza TU-PROJECT-ID
notepad terraform.tfvars
```

### 2.3 Inicializar Terraform
```powershell
terraform init
```

### 2.4 Ver plan de ejecución
```powershell
terraform plan
```

### 2.5 Crear infraestructura
```powershell
terraform apply
```
> Escribe `yes` cuando te pregunte.
> ⏱️ Este proceso toma aproximadamente 10-15 minutos.

### 2.6 Guardar los outputs
```powershell
# Ver los valores de salida
terraform output

# Guardar el comando para kubectl
terraform output kubectl_config_command
```

---

## 🐳 PASO 3: Build y Push de Imagen Docker

### 3.1 Navegar a la carpeta del proyecto
```powershell
cd "c:\Users\cater\Downloads\DOCKER CONTENEDOR\BACKEND_DEVELOMENT\BACKEND_DEVELOMENT\API_REST_CURSOSACADEMICOS"
```

### 3.2 Construir la imagen Docker
```powershell
docker build -t api-cursos-academicos:latest .
```

### 3.3 Etiquetar la imagen para Artifact Registry
```powershell
# REEMPLAZA TU-PROJECT-ID con tu Project ID real
docker tag api-cursos-academicos:latest us-central1-docker.pkg.dev/TU-PROJECT-ID/api-cursos-repo/api-cursos-academicos:latest
```

### 3.4 Subir la imagen a Artifact Registry
```powershell
# REEMPLAZA TU-PROJECT-ID con tu Project ID real
docker push us-central1-docker.pkg.dev/TU-PROJECT-ID/api-cursos-repo/api-cursos-academicos:latest
```

---

## ☸️ PASO 4: Configurar kubectl

### 4.1 Obtener credenciales del clúster
```powershell
# REEMPLAZA TU-PROJECT-ID con tu Project ID real
gcloud container clusters get-credentials gke-cursos-academicos --zone us-central1-a --project TU-PROJECT-ID
```

### 4.2 Verificar conexión
```powershell
kubectl get nodes
```
> Deberías ver 1 nodo con estado "Ready"

---

## 🔑 PASO 5: Configurar Secretos

### 5.1 Codificar tu cadena de conexión en Base64
```powershell
# Tu cadena de conexión completa
$connectionString = "Server=SQL1001.site4now.net;Database=db_ac177a_gestionacademica;User Id=db_ac177a_gestionacademica_admin;Password=TU_PASSWORD_AQUI;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"

# Codificar en Base64
$base64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($connectionString))
Write-Host $base64
```

### 5.2 Actualizar secret.yaml
Edita el archivo `k8s/secret.yaml` y reemplaza el valor de `connection-string` con el valor Base64 generado.

```powershell
notepad k8s\secret.yaml
```

---

## 🚀 PASO 6: Desplegar en Kubernetes

### 6.1 Actualizar deployment.yaml
Edita `k8s/deployment.yaml` y reemplaza `<TU-PROJECT-ID>` con tu Project ID real.

```powershell
notepad k8s\deployment.yaml
```

### 6.2 Aplicar configuraciones
```powershell
# Navegar a la carpeta del proyecto
cd "c:\Users\cater\Downloads\DOCKER CONTENEDOR\BACKEND_DEVELOMENT\BACKEND_DEVELOMENT\API_REST_CURSOSACADEMICOS"

# Aplicar el secret
kubectl apply -f k8s/secret.yaml

# Aplicar el configmap (opcional)
kubectl apply -f k8s/configmap.yaml

# Aplicar el deployment
kubectl apply -f k8s/deployment.yaml

# Aplicar el service (LoadBalancer)
kubectl apply -f k8s/service.yaml
```

### 6.3 Verificar el despliegue
```powershell
# Ver pods
kubectl get pods

# Ver logs del pod (si hay errores)
kubectl logs -f deployment/api-cursos-academicos

# Ver servicios
kubectl get services
```

### 6.4 Obtener IP Externa
```powershell
# Espera unos minutos a que se asigne la IP externa
kubectl get service api-cursos-academicos-lb --watch
```
> La columna `EXTERNAL-IP` mostrará tu IP pública cuando esté lista.
> Presiona Ctrl+C para salir del watch.

---

## 🧪 PASO 7: Probar la API

Una vez que tengas la IP externa, prueba tu API:

```powershell
# Reemplaza EXTERNAL-IP con tu IP
curl http://EXTERNAL-IP/health

# O en el navegador
# http://EXTERNAL-IP/swagger
```

---

## 💰 COSTOS ESTIMADOS

| Recurso | Costo Aproximado/Mes |
|---------|---------------------|
| GKE Cluster Management | ~$74 (puede tener capa gratuita) |
| VM e2-medium (1 nodo) | ~$25 |
| Load Balancer | ~$18 |
| Artifact Registry | ~$0.10/GB |
| **Total Estimado** | ~$50-120/mes |

### Tips para Reducir Costos:
1. Usa Spot VMs (descomenta `spot = true` en main.tf)
2. Apaga el clúster cuando no lo uses
3. Usa Autopilot en lugar de Standard

---

## 🛠️ COMANDOS ÚTILES

### Ver estado del clúster
```powershell
kubectl get all
```

### Ver logs en tiempo real
```powershell
kubectl logs -f deployment/api-cursos-academicos
```

### Reiniciar deployment
```powershell
kubectl rollout restart deployment/api-cursos-academicos
```

### Escalar pods
```powershell
kubectl scale deployment/api-cursos-academicos --replicas=2
```

### Eliminar todo
```powershell
kubectl delete -f k8s/
```

### Destruir infraestructura Terraform
```powershell
cd terraform
terraform destroy
```

---

## ⚠️ TROUBLESHOOTING

### Error: "ImagePullBackOff"
- Verifica que la imagen esté subida correctamente
- Verifica que el nombre de la imagen en deployment.yaml sea correcto

### Error: "CrashLoopBackOff"
- Revisa los logs: `kubectl logs deployment/api-cursos-academicos`
- Verifica la cadena de conexión

### Error: "Pending" en External-IP
- Espera 2-3 minutos
- Verifica que el Load Balancer esté habilitado en tu proyecto

### Error de conexión a SQL Server
- Asegúrate de que `TrustServerCertificate=True` esté en la cadena de conexión
- Verifica que el firewall de MonsterASP permita conexiones desde Google Cloud

---

## 📝 FORMATO DE CADENA DE CONEXIÓN

Para conectarte a tu base de datos en MonsterASP/site4now desde Google Cloud:

```
Server=SQL1001.site4now.net;Database=db_ac177a_gestionacademica;User Id=db_ac177a_gestionacademica_admin;Password=TU_PASSWORD;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
```

**Parámetros importantes:**
- `Encrypt=True` - Encripta la conexión
- `TrustServerCertificate=True` - **CRÍTICO** para conexiones externas
- `MultipleActiveResultSets=True` - Permite múltiples consultas simultáneas

---

## ✅ CHECKLIST FINAL

- [ ] Google Cloud CLI instalado y configurado
- [ ] Terraform instalado
- [ ] Docker Desktop instalado y corriendo
- [ ] Proyecto de Google Cloud creado
- [ ] APIs habilitadas (container, artifactregistry, compute)
- [ ] terraform.tfvars configurado con tu Project ID
- [ ] Infraestructura creada con `terraform apply`
- [ ] Imagen Docker construida y subida
- [ ] kubectl configurado con credenciales del clúster
- [ ] secret.yaml actualizado con tu cadena de conexión en Base64
- [ ] deployment.yaml actualizado con tu Project ID
- [ ] Recursos de Kubernetes aplicados
- [ ] IP externa obtenida y API funcionando

¡Felicitaciones! Tu API está desplegada en Google Cloud. 🎉
