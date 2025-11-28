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
        }
    }
}
