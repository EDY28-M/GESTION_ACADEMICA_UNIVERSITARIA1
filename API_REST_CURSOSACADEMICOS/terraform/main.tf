# ========================================
# MAIN.TF - Recursos de Infraestructura
# ========================================

# ---------------------------------------
# 1. ARTIFACT REGISTRY - Repositorio Docker
# ---------------------------------------
resource "google_artifact_registry_repository" "api_repo" {
  location      = var.region
  repository_id = var.repository_name
  description   = "Repositorio Docker para API Cursos Académicos"
  format        = "DOCKER"

  # Política de limpieza automática (opcional, ahorra espacio)
  cleanup_policy_dry_run = false
}

# ---------------------------------------
# 2. GKE CLUSTER - Clúster de Kubernetes
# ---------------------------------------
resource "google_container_cluster" "primary" {
  name     = var.cluster_name
  location = var.zone  # Usar zona específica es más económico que regional

  # Eliminar el node pool por defecto y crear uno personalizado
  remove_default_node_pool = true
  initial_node_count       = 1

  # Configuración de red
  network    = "default"
  subnetwork = "default"

  # Deshabilitar características que no necesitamos (ahorra costos)
  logging_service    = "none"
  monitoring_service = "none"

  # Configuración de seguridad mínima
  master_auth {
    client_certificate_config {
      issue_client_certificate = false
    }
  }

  # Eliminar el clúster también elimina los node pools
  deletion_protection = false
}

# ---------------------------------------
# 3. NODE POOL - Nodos del Clúster
# ---------------------------------------
resource "google_container_node_pool" "primary_nodes" {
  name       = "${var.cluster_name}-node-pool"
  location   = var.zone
  cluster    = google_container_cluster.primary.name
  node_count = var.node_count

  node_config {
    machine_type = var.machine_type
    disk_size_gb = 30  # Disco mínimo para ahorrar
    disk_type    = "pd-standard"

    # Scopes necesarios
    oauth_scopes = [
      "https://www.googleapis.com/auth/cloud-platform"
    ]

    # Labels para identificar nodos
    labels = {
      env     = "production"
      project = "cursos-academicos"
    }

    # Spot VM para máximo ahorro (opcional, puede ser interrumpido)
    # Descomenta la siguiente línea si quieres usar Spot VMs (70-90% más barato)
    # spot = true
  }

  # Configuración de auto-repair y auto-upgrade
  management {
    auto_repair  = true
    auto_upgrade = true
  }
}
