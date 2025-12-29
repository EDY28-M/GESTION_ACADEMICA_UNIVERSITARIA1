using Microsoft.EntityFrameworkCore;
using API_REST_CURSOSACADEMICOS.Models;

namespace API_REST_CURSOSACADEMICOS.Data
{
    public class GestionAcademicaContext : DbContext
    {
        public GestionAcademicaContext(DbContextOptions<GestionAcademicaContext> options) : base(options)
        {
        }

        public DbSet<Docente> Docentes { get; set; }
        public DbSet<Curso> Cursos { get; set; }
        public DbSet<CursoPrerequisito> CursoPrerequisitos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }
        public DbSet<Estudiante> Estudiantes { get; set; }
        public DbSet<Periodo> Periodos { get; set; }
        public DbSet<Matricula> Matriculas { get; set; }
        public DbSet<Nota> Notas { get; set; }
        public DbSet<Asistencia> Asistencias { get; set; }
        public DbSet<TipoEvaluacion> TiposEvaluacion { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<Horario> Horarios { get; set; }
        public DbSet<TrabajoEncargado> TrabajosEncargados { get; set; }
        public DbSet<TrabajoArchivo> TrabajoArchivos { get; set; }
        public DbSet<TrabajoLink> TrabajoLinks { get; set; }
        public DbSet<TrabajoEntrega> TrabajoEntregas { get; set; }
        public DbSet<TrabajoEntregaArchivo> TrabajoEntregaArchivos { get; set; }
        public DbSet<TrabajoEntregaLink> TrabajoEntregaLinks { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración para Docente
            modelBuilder.Entity<Docente>(entity =>
            {
                entity.ToTable("Docente");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Apellidos).HasColumnName("apellidos").IsRequired().HasMaxLength(100);
                entity.Property(e => e.Nombres).HasColumnName("nombres").IsRequired().HasMaxLength(100);
                entity.Property(e => e.Profesion).HasColumnName("profesion").HasMaxLength(100);
                entity.Property(e => e.FechaNacimiento).HasColumnName("fecha_nacimiento").HasColumnType("date");
                entity.Property(e => e.Correo).HasColumnName("correo").HasMaxLength(100);

                // Índice único para el correo
                entity.HasIndex(e => e.Correo).IsUnique();
            });

            // Configuración para Curso
            modelBuilder.Entity<Curso>(entity =>
            {
                entity.ToTable("Curso");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.NombreCurso).HasColumnName("curso").IsRequired().HasMaxLength(200);
                entity.Property(e => e.Creditos).HasColumnName("creditos").IsRequired();
                entity.Property(e => e.HorasSemanal).HasColumnName("horasSemanal").IsRequired();
                entity.Property(e => e.Ciclo).HasColumnName("ciclo").HasMaxLength(50);
                entity.Property(e => e.IdDocente).HasColumnName("idDocente");

                // Configuración de relación con Docente
                entity.HasOne(c => c.Docente)
                      .WithMany(d => d.Cursos)
                      .HasForeignKey(c => c.IdDocente)
                      .HasConstraintName("FK_Curso_Docente")
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configuración para CursoPrerequisito
            modelBuilder.Entity<CursoPrerequisito>(entity =>
            {
                entity.ToTable("CursoPrerequisito");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.IdCurso).HasColumnName("idCurso").IsRequired();
                entity.Property(e => e.IdCursoPrerequisito).HasColumnName("idCursoPrerequisito").IsRequired();

                // Configuración de relación: Curso -> PrerequisitosRequeridos
                entity.HasOne(cp => cp.CursoDestino)
                      .WithMany(c => c.PrerequisitosRequeridos)
                      .HasForeignKey(cp => cp.IdCurso)
                      .HasConstraintName("FK_CursoPrerequisito_Curso")
                      .OnDelete(DeleteBehavior.Cascade);

                // Configuración de relación: Curso Prerequisito -> EsPrerrequisitoDE
                entity.HasOne(cp => cp.Prerequisito)
                      .WithMany(c => c.EsPrerrequisitoDE)
                      .HasForeignKey(cp => cp.IdCursoPrerequisito)
                      .HasConstraintName("FK_CursoPrerequisito_CursoPrerequisito")
                      .OnDelete(DeleteBehavior.NoAction);

                // Índice único para evitar prerequisitos duplicados
                entity.HasIndex(e => new { e.IdCurso, e.IdCursoPrerequisito }).IsUnique();
            });

            // Configuración para Usuario
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuario");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired().HasMaxLength(255);
                entity.Property(e => e.Nombres).HasColumnName("nombres").IsRequired().HasMaxLength(100);
                entity.Property(e => e.Apellidos).HasColumnName("apellidos").IsRequired().HasMaxLength(100);
                entity.Property(e => e.Rol).HasColumnName("rol").IsRequired().HasMaxLength(50).HasDefaultValue("Usuario");
                entity.Property(e => e.Estado).HasColumnName("estado").IsRequired().HasDefaultValue(true);
                entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.FechaActualizacion).HasColumnName("fecha_actualizacion").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UltimoAcceso).HasColumnName("ultimo_acceso");
                entity.Property(e => e.RefreshToken).HasColumnName("refresh_token").HasMaxLength(500);
                entity.Property(e => e.RefreshTokenExpiry).HasColumnName("refresh_token_expiry");

                // Índice único para email
                entity.HasIndex(e => e.Email).IsUnique();
                
                // Índices para mejorar rendimiento
                entity.HasIndex(e => e.Estado);
                entity.HasIndex(e => e.Rol);

                // Ignorar propiedad calculada
                entity.Ignore(e => e.NombreCompleto);
            });

            // Configuración para Asistencia
            modelBuilder.Entity<Asistencia>(entity =>
            {
                entity.ToTable("Asistencia");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.IdEstudiante).HasColumnName("idEstudiante").IsRequired();
                entity.Property(e => e.IdCurso).HasColumnName("idCurso").IsRequired();
                entity.Property(e => e.Fecha).HasColumnName("fecha").HasColumnType("date").IsRequired();
                entity.Property(e => e.Presente).HasColumnName("presente").IsRequired();
                entity.Property(e => e.Observaciones).HasColumnName("observaciones").HasMaxLength(500);
                entity.Property(e => e.FechaRegistro).HasColumnName("fecha_registro").HasDefaultValueSql("GETDATE()");

                // Configuración de relaciones
                entity.HasOne(a => a.Estudiante)
                      .WithMany()
                      .HasForeignKey(a => a.IdEstudiante)
                      .HasConstraintName("FK_Asistencia_Estudiante")
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Curso)
                      .WithMany()
                      .HasForeignKey(a => a.IdCurso)
                      .HasConstraintName("FK_Asistencia_Curso")
                      .OnDelete(DeleteBehavior.Cascade);

                // Índices
                entity.HasIndex(e => e.IdEstudiante);
                entity.HasIndex(e => e.IdCurso);
                entity.HasIndex(e => e.Fecha);
                
                // Índice único compuesto para evitar duplicados
                entity.HasIndex(e => new { e.IdEstudiante, e.IdCurso, e.Fecha }).IsUnique();
            });

            // Configuración para Estudiante - Precisión de decimales
            modelBuilder.Entity<Estudiante>(entity =>
            {
                entity.Property(e => e.PromedioAcumulado).HasColumnType("decimal(5,2)");
                entity.Property(e => e.PromedioSemestral).HasColumnType("decimal(5,2)");
            });

            // Configuración para Matricula - Precisión de decimales
            modelBuilder.Entity<Matricula>(entity =>
            {
                entity.Property(e => e.PromedioFinal).HasColumnType("decimal(5,2)");
            });

            // Configuración para Nota
            modelBuilder.Entity<Nota>(entity =>
            {
                entity.ToTable("Nota");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.IdMatricula).HasColumnName("idMatricula").IsRequired();
                entity.Property(e => e.TipoEvaluacion).HasColumnName("tipo_evaluacion").IsRequired().HasMaxLength(50);
                entity.Property(e => e.NotaValor).HasColumnName("nota").HasColumnType("decimal(5,2)").IsRequired();
                entity.Property(e => e.Peso).HasColumnName("peso").HasColumnType("decimal(5,2)");
                entity.Property(e => e.Fecha).HasColumnName("fecha_evaluacion").HasColumnType("date");
                entity.Property(e => e.FechaRegistro).HasColumnName("fecha_registro").HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.Observaciones).HasColumnName("observaciones").HasMaxLength(500);

                // Configuración de relación con Matricula
                entity.HasOne(n => n.Matricula)
                      .WithMany(m => m.Notas)
                      .HasForeignKey(n => n.IdMatricula)
                      .HasConstraintName("FK_Nota_Matricula")
                      .OnDelete(DeleteBehavior.Cascade);

                // Índices
                entity.HasIndex(e => e.IdMatricula);
                entity.HasIndex(e => e.TipoEvaluacion);
            });

            // Configuración para TipoEvaluacion
            modelBuilder.Entity<TipoEvaluacion>(entity =>
            {
                entity.ToTable("TipoEvaluacion");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.IdCurso).HasColumnName("id_curso");
                entity.Property(e => e.Nombre).HasColumnName("nombre").IsRequired().HasMaxLength(100);
                entity.Property(e => e.Peso).HasColumnName("peso").HasColumnType("decimal(5,2)");
                entity.Property(e => e.Orden).HasColumnName("orden");
                entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
                entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasDefaultValueSql("GETDATE()");

                // Configuración de relación con Curso
                entity.HasOne(t => t.Curso)
                      .WithMany()
                      .HasForeignKey(t => t.IdCurso)
                      .HasConstraintName("FK_TipoEvaluacion_Curso")
                      .OnDelete(DeleteBehavior.Cascade);

                // Índices
                entity.HasIndex(e => e.IdCurso);
                entity.HasIndex(e => new { e.IdCurso, e.Orden });
            });

            // Configuración para Horario
            modelBuilder.Entity<Horario>(entity =>
            {
                entity.ToTable("Horario");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.IdCurso).HasColumnName("idCurso");
                entity.Property(e => e.DiaSemana).HasColumnName("dia_semana").IsRequired();
                entity.Property(e => e.HoraInicio).HasColumnName("hora_inicio").IsRequired();
                entity.Property(e => e.HoraFin).HasColumnName("hora_fin").IsRequired();
                entity.Property(e => e.Aula).HasColumnName("aula").HasMaxLength(50);
                entity.Property(e => e.Tipo).HasColumnName("tipo").HasMaxLength(20).IsRequired();

                // Relación con Curso
                entity.HasOne(d => d.Curso)
                      .WithMany()
                      .HasForeignKey(d => d.IdCurso)
                      .HasConstraintName("FK_Horario_Curso")
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración para TrabajoEncargado
            modelBuilder.Entity<TrabajoEncargado>(entity =>
            {
                entity.ToTable("TrabajoEncargado");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.IdCurso).HasColumnName("idCurso").IsRequired();
                entity.Property(e => e.IdDocente).HasColumnName("idDocente").IsRequired();
                entity.Property(e => e.Titulo).HasColumnName("titulo").IsRequired().HasMaxLength(200);
                entity.Property(e => e.Descripcion).HasColumnName("descripcion");
                entity.Property(e => e.FechaCreacion).HasColumnName("fechaCreacion").IsRequired();
                entity.Property(e => e.FechaLimite).HasColumnName("fechaLimite").IsRequired();
                entity.Property(e => e.Activo).HasColumnName("activo").HasDefaultValue(true);
                entity.Property(e => e.FechaActualizacion).HasColumnName("fechaActualizacion");
                entity.Property(e => e.IdTipoEvaluacion).HasColumnName("idTipoEvaluacion");
                entity.Property(e => e.NumeroTrabajo).HasColumnName("numeroTrabajo");
                entity.Property(e => e.TotalTrabajos).HasColumnName("totalTrabajos");
                entity.Property(e => e.PesoIndividual).HasColumnName("pesoIndividual").HasColumnType("decimal(5,2)");

                entity.HasOne(t => t.Curso)
                      .WithMany()
                      .HasForeignKey(t => t.IdCurso)
                      .HasConstraintName("FK_TrabajoEncargado_Curso")
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(t => t.Docente)
                      .WithMany()
                      .HasForeignKey(t => t.IdDocente)
                      .HasConstraintName("FK_TrabajoEncargado_Docente")
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.TipoEvaluacion)
                      .WithMany()
                      .HasForeignKey(t => t.IdTipoEvaluacion)
                      .HasConstraintName("FK_TrabajoEncargado_TipoEvaluacion")
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.IdCurso);
                entity.HasIndex(e => e.IdDocente);
                entity.HasIndex(e => e.FechaLimite);
            });

            // Configuración para TrabajoArchivo
            modelBuilder.Entity<TrabajoArchivo>(entity =>
            {
                entity.ToTable("TrabajoArchivo");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.IdTrabajo).HasColumnName("idTrabajo").IsRequired();
                entity.Property(e => e.NombreArchivo).HasColumnName("nombreArchivo").IsRequired().HasMaxLength(500);
                entity.Property(e => e.RutaArchivo).HasColumnName("rutaArchivo").IsRequired().HasMaxLength(1000);
                entity.Property(e => e.TipoArchivo).HasColumnName("tipoArchivo").HasMaxLength(100);
                entity.Property(e => e.Tamaño).HasColumnName("tamaño");
                entity.Property(e => e.FechaSubida).HasColumnName("fechaSubida").IsRequired();

                entity.HasOne(a => a.Trabajo)
                      .WithMany(t => t.Archivos)
                      .HasForeignKey(a => a.IdTrabajo)
                      .HasConstraintName("FK_TrabajoArchivo_TrabajoEncargado")
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.IdTrabajo);
            });

            // Configuración para TrabajoLink
            modelBuilder.Entity<TrabajoLink>(entity =>
            {
                entity.ToTable("TrabajoLink");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.IdTrabajo).HasColumnName("idTrabajo").IsRequired();
                entity.Property(e => e.Url).HasColumnName("url").IsRequired().HasMaxLength(500);
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(200);
                entity.Property(e => e.FechaCreacion).HasColumnName("fechaCreacion").IsRequired();

                entity.HasOne(l => l.Trabajo)
                      .WithMany(t => t.Links)
                      .HasForeignKey(l => l.IdTrabajo)
                      .HasConstraintName("FK_TrabajoLink_TrabajoEncargado")
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.IdTrabajo);
            });

            // Configuración para TrabajoEntrega
            modelBuilder.Entity<TrabajoEntrega>(entity =>
            {
                entity.ToTable("TrabajoEntrega");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.IdTrabajo).HasColumnName("idTrabajo").IsRequired();
                entity.Property(e => e.IdEstudiante).HasColumnName("idEstudiante").IsRequired();
                entity.Property(e => e.Comentario).HasColumnName("comentario");
                entity.Property(e => e.FechaEntrega).HasColumnName("fechaEntrega").IsRequired();
                entity.Property(e => e.Calificacion).HasColumnName("calificacion").HasColumnType("decimal(5,2)");
                entity.Property(e => e.Observaciones).HasColumnName("observaciones");
                entity.Property(e => e.FechaCalificacion).HasColumnName("fechaCalificacion");
                entity.Property(e => e.EntregadoTarde).HasColumnName("entregadoTarde").HasDefaultValue(false);

                entity.HasOne(e => e.Trabajo)
                      .WithMany(t => t.Entregas)
                      .HasForeignKey(e => e.IdTrabajo)
                      .HasConstraintName("FK_TrabajoEntrega_TrabajoEncargado")
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Estudiante)
                      .WithMany()
                      .HasForeignKey(e => e.IdEstudiante)
                      .HasConstraintName("FK_TrabajoEntrega_Estudiante")
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.IdTrabajo);
                entity.HasIndex(e => e.IdEstudiante);
                entity.HasIndex(e => new { e.IdTrabajo, e.IdEstudiante }).IsUnique();
            });

            // Configuración para TrabajoEntregaArchivo
            modelBuilder.Entity<TrabajoEntregaArchivo>(entity =>
            {
                entity.ToTable("TrabajoEntregaArchivo");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.IdEntrega).HasColumnName("idEntrega").IsRequired();
                entity.Property(e => e.NombreArchivo).HasColumnName("nombreArchivo").IsRequired().HasMaxLength(500);
                entity.Property(e => e.RutaArchivo).HasColumnName("rutaArchivo").IsRequired().HasMaxLength(1000);
                entity.Property(e => e.TipoArchivo).HasColumnName("tipoArchivo").HasMaxLength(100);
                entity.Property(e => e.Tamaño).HasColumnName("tamaño");
                entity.Property(e => e.FechaSubida).HasColumnName("fechaSubida").IsRequired();

                entity.HasOne(a => a.Entrega)
                      .WithMany(e => e.Archivos)
                      .HasForeignKey(a => a.IdEntrega)
                      .HasConstraintName("FK_TrabajoEntregaArchivo_TrabajoEntrega")
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.IdEntrega);
            });

            // Configuración para TrabajoEntregaLink
            modelBuilder.Entity<TrabajoEntregaLink>(entity =>
            {
                entity.ToTable("TrabajoEntregaLink");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.IdEntrega).HasColumnName("idEntrega").IsRequired();
                entity.Property(e => e.Url).HasColumnName("url").IsRequired().HasMaxLength(500);
                entity.Property(e => e.Descripcion).HasColumnName("descripcion").HasMaxLength(200);
                entity.Property(e => e.FechaCreacion).HasColumnName("fechaCreacion").IsRequired();

                entity.HasOne(l => l.Entrega)
                      .WithMany(e => e.Links)
                      .HasForeignKey(l => l.IdEntrega)
                      .HasConstraintName("FK_TrabajoEntregaLink_TrabajoEntrega")
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.IdEntrega);
            });

        }
    }
}
