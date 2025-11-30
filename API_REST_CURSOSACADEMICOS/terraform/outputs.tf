# ========================================
# OUTPUTS.TF - Valores de Salida
# ========================================

# URL del Artifact Registry
output "artifact_registry_url" {
  description = "URL del repositorio de Artifact Registry"
  value       = "${var.region}-docker.pkg.dev/${var.project_id}/${var.repository_name}"
}

# Nombre del clúster
output "cluster_name" {
  description = "Nombre del clúster GKE"
  value       = google_container_cluster.primary.name
}

# Zona del clúster
output "cluster_zone" {
  description = "Zona del clúster GKE"
  value       = google_container_cluster.primary.location
}

# Comando para configurar kubectl
output "kubectl_config_command" {
  description = "Comando para configurar kubectl con el clúster"
  value       = "gcloud container clusters get-credentials ${google_container_cluster.primary.name} --zone ${google_container_cluster.primary.location} --project ${var.project_id}"
}

# URL completa de la imagen Docker
output "docker_image_url" {
  description = "URL para la imagen Docker (usa esta para el tag)"
  value       = "${var.region}-docker.pkg.dev/${var.project_id}/${var.repository_name}/api-cursos-academicos:latest"
}
