# 🚀 Guía Rápida de Configuración - Deploy a Cloud Run

## ⚡ Pasos Rápidos

### 1. Ejecuta el Script de Verificación

```powershell
cd BACKEND_DEVELOMENT
.\verificar-configuracion-gcp.ps1
```

Este script te guiará paso a paso para verificar tu configuración de GCP.

### 2. Configura los Secrets en GitHub

Ve a tu repositorio → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**

#### Secrets Requeridos:

1. **GCP_SA_KEY** (Service Account JSON)
   - Ve a: [Google Cloud Console - Service Accounts](https://console.cloud.google.com/iam-admin/serviceaccounts)
   - Busca o crea: `github-actions-deployer@TU_PROJECT_ID.iam.gserviceaccount.com`
   - Crea una clave JSON y copia TODO el contenido
   - Pega el JSON completo en el secret `GCP_SA_KEY`

2. **GCP_PROJECT_ID**
   - Valor: `flash-adapter-424617-u4` (o tu Project ID)
   - **NO uses el nombre del proyecto**, solo el ID

3. **CLOUD_RUN_CONNECTION_STRING**
   - Connection string de tu base de datos SQL Server
   - Formato: `Server=tcp:SERVIDOR,1433;Database=GestionAcademica;User Id=USUARIO;Password=CONTRASEÑA;TrustServerCertificate=True;Encrypt=True;`

4. **JWT_SECRET_KEY**
   - Genera una clave secreta (mínimo 32 caracteres)
   - Puedes usar: `.\generate-jwt-key.ps1`
   - O genera manualmente:
   ```powershell
   $bytes = New-Object byte[] 64
   [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
   [System.Convert]::ToBase64String($bytes)
   ```

5. **JWT_ISSUER**
   - Valor: `GestionAcademicaAPI`

6. **JWT_AUDIENCE**
   - Valor: `GestionAcademicaClients`

### 3. Verifica que los Secrets Estén Configurados

En GitHub → **Settings** → **Secrets and variables** → **Actions**, deberías ver estos 6 secrets:

- ✅ `GCP_SA_KEY`
- ✅ `GCP_PROJECT_ID`
- ✅ `CLOUD_RUN_CONNECTION_STRING`
- ✅ `JWT_SECRET_KEY`
- ✅ `JWT_ISSUER`
- ✅ `JWT_AUDIENCE`

### 4. Haz Push a Main/Master

```bash
git add .
git commit -m "Configurar deployment"
git push origin main
```

El workflow de GitHub Actions se ejecutará automáticamente y validará que todos los secrets estén configurados antes de desplegar.

## 🔍 Verificación Paso a Paso de GCP

### Paso 1: Autenticación

```powershell
# Inicia sesión en Google Cloud
gcloud auth login

# Verifica que estás autenticado
gcloud auth list
```

### Paso 2: Seleccionar Proyecto

```powershell
# Lista proyectos disponibles
gcloud projects list

# Selecciona tu proyecto
gcloud config set project flash-adapter-424617-u4

# Verifica el proyecto seleccionado
gcloud config get-value project
```

### Paso 3: Verificar/Crear Service Account

```powershell
# Lista service accounts
gcloud iam service-accounts list

# Si no existe, crea uno nuevo
gcloud iam service-accounts create github-actions-deployer \
  --display-name="GitHub Actions Deployer" \
  --project=flash-adapter-424617-u4

# Otorga permisos necesarios
gcloud projects add-iam-policy-binding flash-adapter-424617-u4 \
  --member="serviceAccount:github-actions-deployer@flash-adapter-424617-u4.iam.gserviceaccount.com" \
  --role="roles/run.admin"

gcloud projects add-iam-policy-binding flash-adapter-424617-u4 \
  --member="serviceAccount:github-actions-deployer@flash-adapter-424617-u4.iam.gserviceaccount.com" \
  --role="roles/iam.serviceAccountUser"

gcloud projects add-iam-policy-binding flash-adapter-424617-u4 \
  --member="serviceAccount:github-actions-deployer@flash-adapter-424617-u4.iam.gserviceaccount.com" \
  --role="roles/storage.admin"

# Crea y descarga la clave JSON
gcloud iam service-accounts keys create github-actions-key.json \
  --iam-account=github-actions-deployer@flash-adapter-424617-u4.iam.gserviceaccount.com \
  --project=flash-adapter-424617-u4
```

### Paso 4: Habilitar APIs Necesarias

```powershell
# Habilitar Cloud Run API
gcloud services enable run.googleapis.com --project=flash-adapter-424617-u4

# Habilitar Artifact Registry API
gcloud services enable artifactregistry.googleapis.com --project=flash-adapter-424617-u4

# Habilitar Cloud Build API (si usas Cloud Build)
gcloud services enable cloudbuild.googleapis.com --project=flash-adapter-424617-u4
```

### Paso 5: Crear Artifact Registry (si no existe)

```powershell
# Lista repositorios existentes
gcloud artifacts repositories list --project=flash-adapter-424617-u4

# Si no existe, crea uno nuevo
gcloud artifacts repositories create cloud-run-source-deploy \
  --repository-format=docker \
  --location=us-central1 \
  --project=flash-adapter-424617-u4
```

## ❌ Solución de Problemas Comunes

### Error: "Container failed to start"

**Causa**: Probablemente faltan secrets o están vacíos.

**Solución**:
1. Verifica que todos los secrets estén configurados en GitHub
2. Ejecuta el workflow nuevamente (el nuevo workflow validará los secrets antes de desplegar)
3. Revisa los logs en Cloud Run Console

### Error: "JWT SecretKey no configurada"

**Causa**: El secret `JWT_SECRET_KEY` no está configurado o está vacío.

**Solución**:
1. Genera una nueva clave: `.\generate-jwt-key.ps1`
2. Copia la clave generada
3. Ve a GitHub → Settings → Secrets → Actions
4. Crea o actualiza el secret `JWT_SECRET_KEY` con la clave generada

### Error: "The requested resource is not valid"

**Causa**: El Project ID es incorrecto o no tienes permisos.

**Solución**:
1. Verifica que el `GCP_PROJECT_ID` sea correcto (no el nombre, solo el ID)
2. Verifica que el Service Account tenga los permisos necesarios
3. Verifica que las APIs estén habilitadas

### Error: "Service Account does not have permission"

**Causa**: El Service Account no tiene los roles necesarios.

**Solución**:
1. Ejecuta los comandos del Paso 3 arriba para otorgar los permisos
2. O desde la consola de GCP:
   - Ve a IAM & Admin → IAM
   - Busca el Service Account
   - Edita y agrega los roles:
     - Cloud Run Admin
     - Service Account User
     - Storage Admin

## 📚 Recursos Adicionales

- **Guía completa de secrets**: `GITHUB_SECRETS_GUIDE.md`
- **Script de verificación**: `verificar-configuracion-gcp.ps1`
- **Documentación de Cloud Run**: https://cloud.google.com/run/docs
- **Documentación de GitHub Actions**: https://docs.github.com/en/actions
