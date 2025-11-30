# ========================================
# VARIABLES.TF - Variables de Configuración
# ========================================

variable "project_id" {
  description = "ID del proyecto en Google Cloud"
  type        = string
  # IMPORTANTE: Reemplaza con tu Project ID de Google Cloud
  # Lo encuentras en: https://console.cloud.google.com/
}

variable "region" {
  description = "Región de Google Cloud"
  type        = string
  default     = "us-central1"  # Región económica
}

variable "zone" {
  description = "Zona de Google Cloud"
  type        = string
  default     = "us-central1-a"
}

variable "cluster_name" {
  description = "Nombre del clúster GKE"
  type        = string
  default     = "gke-cursos-academicos"
}

variable "repository_name" {
  description = "Nombre del repositorio en Artifact Registry"
  type        = string
  default     = "api-cursos-repo"
}

variable "machine_type" {
  description = "Tipo de máquina para los nodos"
  type        = string
  default     = "e2-medium"  # 2 vCPU, 4GB RAM - Económico
}

variable "node_count" {
  description = "Número de nodos en el clúster"
  type        = number
  default     = 1  # Mínimo para ahorrar costos
}
