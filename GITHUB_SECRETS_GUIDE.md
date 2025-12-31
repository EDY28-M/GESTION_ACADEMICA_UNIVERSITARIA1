# 🔐 Guía de Configuración de Secrets para GitHub Actions

Esta guía te explica cómo configurar todos los secrets necesarios en GitHub para desplegar a Google Cloud Run.

## 📋 Secrets Requeridos en GitHub

Ve a tu repositorio en GitHub → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**

### 1. **GCP_SA_KEY** (JSON de Service Account)
- **Nombre**: `GCP_SA_KEY`
- **Descripción**: Credenciales JSON completas del Service Account de GCP con permisos para Cloud Run y Artifact Registry
- **Cómo obtenerlo**:
  1. Ve a [Google Cloud Console](https://console.cloud.google.com/)
  2. IAM & Admin → Service Accounts
  3. Crea o selecciona un Service Account
  4. Roles necesarios:
     - `Cloud Run Admin`
     - `Service Account User`
     - `Storage Admin` (para Artifact Registry)
  5. Ve a la pestaña "Keys" → "Add Key" → "Create new key" → JSON
  6. Copia TODO el contenido del JSON (incluyendo llaves `{` y `}`)
- **Ejemplo**:
```json
{
  "type": "service_account",
  "project_id": "flash-adapter-424617-u4",
  "private_key_id": "...",
  "private_key": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n",
  "client_email": "github-actions@flash-adapter-424617-u4.iam.gserviceaccount.com",
  ...
}
```

### 2. **GCP_PROJECT_ID**
- **Nombre**: `GCP_PROJECT_ID`
- **Descripción**: ID del proyecto de Google Cloud
- **Valor**: `flash-adapter-424617-u4`

### 3. **CLOUD_RUN_CONNECTION_STRING**
- **Nombre**: `CLOUD_RUN_CONNECTION_STRING`
- **Descripción**: Connection string de SQL Server para Cloud Run
- **Formato**: 
```
Server=tcp:TU_SERVIDOR.database.windows.net,1433;Database=GestionAcademica;User Id=TU_USUARIO;Password=TU_PASSWORD;TrustServerCertificate=True;Encrypt=True;
```
- **Nota**: Si usas SQL Server en GCP (Cloud SQL), el formato será:
```
Server=/cloudsql/PROJECT_ID:REGION:INSTANCE_NAME;Database=GestionAcademica;User Id=sa;Password=TU_PASSWORD;TrustServerCertificate=True;
```

### 4. **JWT_SECRET_KEY** ⚠️ IMPORTANTE
- **Nombre**: `JWT_SECRET_KEY`
- **Descripción**: Clave secreta para firmar tokens JWT (mínimo 32 caracteres, recomendado 64+)
- **Cómo generarla** (PowerShell):
```powershell
# Opción 1: Base64 (recomendada)
$bytes = New-Object byte[] 64
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
[System.Convert]::ToBase64String($bytes)

# Opción 2: Alfanumérica con símbolos
$chars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*'
-join ($chars.ToCharArray() | Get-Random -Count 64)
```
- **Ejemplo** (64 caracteres): `Kj9$mP2#vL8@nQ4!wR6&tY7*uI0^oP3%eA5$dF1+gH9=zX2#cV4%`

### 5. **JWT_ISSUER**
- **Nombre**: `JWT_ISSUER`
- **Descripción**: Emisor del token JWT
- **Valor recomendado**: `GestionAcademicaAPI`

### 6. **JWT_AUDIENCE**
- **Nombre**: `JWT_AUDIENCE`
- **Descripción**: Audiencia del token JWT
- **Valor recomendado**: `GestionAcademicaClients`

## ✅ Verificación de Secrets Configurados

Después de agregar todos los secrets, deberías tener estos 6 secrets en GitHub:

1. ✅ `GCP_SA_KEY`
2. ✅ `GCP_PROJECT_ID`
3. ✅ `CLOUD_RUN_CONNECTION_STRING`
4. ✅ `JWT_SECRET_KEY`
5. ✅ `JWT_ISSUER`
6. ✅ `JWT_AUDIENCE`

## 🔍 Verificar que los Secrets Están Configurados

1. Ve a tu repositorio en GitHub
2. **Settings** → **Secrets and variables** → **Actions**
3. Deberías ver los 6 secrets listados

## ⚠️ Troubleshooting

### Error: "JWT SecretKey no configurada"
- **Causa**: Falta el secret `JWT_SECRET_KEY` o está vacío
- **Solución**: Agrega el secret con una clave de al menos 32 caracteres

### Error: "Container failed to start"
- **Causa**: Probablemente falta alguna variable de entorno crítica
- **Solución**: Verifica que todos los secrets estén configurados correctamente
- **Diagnóstico**: Revisa los logs en Cloud Run Console para ver el error exacto

### Error: "Connection string no válida"
- **Causa**: El formato de `CLOUD_RUN_CONNECTION_STRING` es incorrecto
- **Solución**: Verifica que el connection string tenga el formato correcto y que las credenciales sean válidas

## 📝 Notas Importantes

- **NO** compartas estos secrets públicamente
- **NO** los subas al repositorio (están en `.gitignore`)
- Los secrets se inyectan automáticamente en el workflow durante el despliegue
- Si cambias un secret, necesitas hacer un nuevo despliegue para que tome efecto
