# 🔑 Cómo Obtener GCP_SA_KEY

## Resumen

**JWT_SECRET_KEY** y **GCP_SA_KEY** son DIFERENTES:

- **JWT_SECRET_KEY**: Ya la generaste ✅ (`/H+xqNTEoK74CZp2834/iJJGF+gTzJKSzNYKUwhNtYFmiyXYfAMHFWkxvFmTSkRGhG9VTQjbZkvNjJrXX4eYmQ==`)
- **GCP_SA_KEY**: Es un archivo JSON que necesitas descargar del Service Account

## Pasos para Obtener GCP_SA_KEY

### Opción 1: Desde Google Cloud Console (Web)

1. Ve a: https://console.cloud.google.com/iam-admin/serviceaccounts?project=flash-adapter-424617-u4

2. Busca el Service Account: `github-actions-deployer@flash-adapter-424617-u4.iam.gserviceaccount.com`

3. Haz clic en el nombre del Service Account

4. Ve a la pestaña **"KEYS"** (o "Claves" en español)

5. Haz clic en **"ADD KEY"** → **"Create new key"**

6. Selecciona formato: **JSON**

7. Haz clic en **"CREATE"**

8. Se descargará automáticamente un archivo JSON (algo como `flash-adapter-424617-u4-xxxxx.json`)

9. **Abre el archivo JSON** con un editor de texto (Notepad, VS Code, etc.)

10. **Copia TODO el contenido** del archivo (desde `{` hasta `}`)

### Opción 2: Desde PowerShell (Línea de comandos)

```powershell
# 1. Asegúrate de estar autenticado
gcloud auth login

# 2. Selecciona tu proyecto
gcloud config set project flash-adapter-424617-u4

# 3. Crea y descarga la clave JSON
gcloud iam service-accounts keys create github-actions-key.json `
  --iam-account=github-actions-deployer@flash-adapter-424617-u4.iam.gserviceaccount.com

# 4. Abre el archivo para ver su contenido
notepad github-actions-key.json

# O muestra el contenido en la terminal
Get-Content github-actions-key.json
```

## Ejemplo de cómo se ve GCP_SA_KEY

El archivo JSON debería verse así (con valores reales):

```json
{
  "type": "service_account",
  "project_id": "flash-adapter-424617-u4",
  "private_key_id": "abc123...",
  "private_key": "-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC...\n-----END PRIVATE KEY-----\n",
  "client_email": "github-actions-deployer@flash-adapter-424617-u4.iam.gserviceaccount.com",
  "client_id": "123456789",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token",
  "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
  "client_x509_cert_url": "https://www.googleapis.com/robot/v1/metadata/x509/..."
}
```

## Configurar en GitHub Secrets

Una vez que tengas el JSON:

1. Ve a tu repositorio en GitHub
2. **Settings** → **Secrets and variables** → **Actions**
3. Haz clic en **"New repository secret"**
4. Nombre: `GCP_SA_KEY`
5. Valor: Pega TODO el contenido del archivo JSON (desde `{` hasta `}`)
6. Haz clic en **"Add secret"**

## Resumen de Secrets Necesarios

| Secret | ¿Qué es? | ¿Cómo obtenerlo? |
|--------|----------|------------------|
| **GCP_SA_KEY** | JSON del Service Account | Descargar desde Google Cloud Console |
| **GCP_PROJECT_ID** | ID del proyecto | `flash-adapter-424617-u4` |
| **JWT_SECRET_KEY** | Clave para tokens JWT | Ya generada: `/H+xqNTEoK74CZp2834/iJJGF+gTzJKSzNYKUwhNtYFmiyXYfAMHFWkxvFmTSkRGhG9VTQjbZkvNjJrXX4eYmQ==` |
| **JWT_ISSUER** | (Opcional) | `GestionAcademicaAPI` (valor por defecto) |
| **JWT_AUDIENCE** | (Opcional) | `GestionAcademicaClients` (valor por defecto) |
| **CLOUD_RUN_CONNECTION_STRING** | (Opcional) | Connection string de tu base de datos |

## ⚠️ Importante

- **NO** compartas estos secrets públicamente
- **NO** los subas al repositorio
- Guarda el archivo JSON del Service Account en un lugar seguro
- Si pierdes el JSON, puedes crear uno nuevo (el anterior seguirá funcionando hasta que lo elimines)
