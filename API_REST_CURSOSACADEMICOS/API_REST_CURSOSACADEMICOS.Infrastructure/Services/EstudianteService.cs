using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API_REST_CURSOSACADEMICOS.Services
{
    public class EstudianteService : IEstudianteService
    {
        private readonly GestionAcademicaContext _context;

        public EstudianteService(GestionAcademicaContext context)
        {
            _context = context;
        }

        public async Task<EstudianteDto?> GetByUsuarioIdAsync(int usuarioId)
        {
            var estudiante = await _context.Estudiantes
                .Include(e => e.Usuario)
                .FirstOrDefaultAsync(e => e.IdUsuario == usuarioId);

            if (estudiante == null) return null;

            return new EstudianteDto
            {
                Id = estudiante.Id,
                Codigo = estudiante.Codigo,
                NombreCompleto = $"{estudiante.Usuario?.Nombres} {estudiante.Usuario?.Apellidos}",
                Email = estudiante.Usuario?.Email ?? string.Empty,
                
                // Información personal adicional
                Nombres = estudiante.Nombres,
                Apellidos = estudiante.Apellidos,
                Dni = estudiante.Dni,
                FechaNacimiento = estudiante.FechaNacimiento,
                Correo = estudiante.Correo,
                Telefono = estudiante.Telefono,
                Direccion = estudiante.Direccion,
                
                // Información académica
                CicloActual = estudiante.CicloActual,
                CreditosAcumulados = estudiante.CreditosAcumulados,
                PromedioAcumulado = estudiante.PromedioAcumulado,
                PromedioSemestral = estudiante.PromedioSemestral,
                Carrera = estudiante.Carrera,
                FechaIngreso = estudiante.FechaIngreso,
                Estado = estudiante.Estado
            };
        }

        public async Task<List<CursoDisponibleDto>> GetCursosDisponiblesAsync(int cicloActual, int idPeriodo)
        {
            // Verificar que el período existe
            var periodo = await _context.Periodos.FindAsync(idPeriodo);
            if (periodo == null)
            {
                return new List<CursoDisponibleDto>();
            }

            // Obtener el estudiante por ciclo actual (asumiendo que viene del contexto del usuario)
            // NOTA: Este método necesita el idEstudiante para usar la función SQL
            // Por ahora, mantenemos la lógica original pero filtrada por ciclo y cursos no aprobados
            
            var cursos = await _context.Cursos
                .Include(c => c.Docente)
                .Where(c => c.Ciclo == cicloActual)
                .ToListAsync();

            return cursos.Select(curso => new CursoDisponibleDto
            {
                Id = curso.Id,
                Codigo = curso.Codigo,
                NombreCurso = curso.NombreCurso,
                Creditos = curso.Creditos,
                HorasSemanal = curso.HorasSemanal,
                Ciclo = curso.Ciclo,
                NombreDocente = curso.Docente != null 
                    ? $"{curso.Docente.Nombres} {curso.Docente.Apellidos}" 
                    : "Sin asignar",
                YaMatriculado = false,
                Disponible = true,
                MotivoNoDisponible = string.Empty
            }).ToList();
        }

        public async Task<List<CursoDisponibleDto>> GetCursosDisponiblesPorEstudianteAsync(int idEstudiante)
        {
            // Obtener el período activo
            var periodoActivo = await _context.Periodos.FirstOrDefaultAsync(p => p.Activo);
            if (periodoActivo == null)
            {
                return new List<CursoDisponibleDto>();
            }

            // Calcular créditos aprobados del estudiante para validación de Práctica Profesional
            var creditosAprobados = await _context.Matriculas
                .Include(m => m.Curso)
                .Where(m => m.IdEstudiante == idEstudiante &&
                           m.PromedioFinal.HasValue &&
                           m.PromedioFinal.Value >= 11 &&
                           m.Estado != "Retirado")
                .SumAsync(m => m.Curso != null ? m.Curso.Creditos : 0);

            const int CREDITOS_MINIMOS_PRACTICA = 140;

            // Usar la función SQL fn_CursosDisponibles que filtra por ciclo actual
            // y excluye cursos ya aprobados
            var cursos = await _context.Cursos
                .FromSqlRaw(@"
                    SELECT c.* 
                    FROM dbo.fn_CursosDisponibles({0}) f
                    INNER JOIN dbo.Curso c ON c.id = f.id
                ", idEstudiante)
                .Include(c => c.Docente)
                .ToListAsync();

            return cursos.Select(curso => {
                // Validación especial para Práctica Pre Profesional
                bool esPracticaProfesional = (curso.NombreCurso.ToLower().Contains("práctica") || 
                                              curso.NombreCurso.ToLower().Contains("practica") ||
                                              curso.NombreCurso.ToLower().Contains("prácticas")) && 
                                             curso.NombreCurso.ToLower().Contains("profesional");
                
                bool disponible = true;
                string motivoNoDisponible = string.Empty;

                if (esPracticaProfesional && creditosAprobados < CREDITOS_MINIMOS_PRACTICA)
                {
                    disponible = false;
                    motivoNoDisponible = $"Requiere {CREDITOS_MINIMOS_PRACTICA} créditos aprobados (tienes {creditosAprobados})";
                }

                return new CursoDisponibleDto
                {
                    Id = curso.Id,
                    Codigo = curso.Codigo,
                    NombreCurso = curso.NombreCurso,
                    Creditos = curso.Creditos,
                    HorasSemanal = curso.HorasSemanal,
                    Ciclo = curso.Ciclo,
                    NombreDocente = curso.Docente != null 
                        ? $"{curso.Docente.Nombres} {curso.Docente.Apellidos}" 
                        : "Sin asignar",
                    YaMatriculado = false,
                    Disponible = disponible,
                    MotivoNoDisponible = motivoNoDisponible,
                    EstudiantesMatriculados = _context.Matriculas
                        .Count(m => m.IdCurso == curso.Id && 
                                   m.IdPeriodo == periodoActivo.Id && 
                                   m.Estado == "Matriculado"),
                    CapacidadMaxima = 30
                };
            }).ToList();
        }

        private bool DeterminarDisponibilidad(Curso curso, int cicloActual, List<int> cursosMatriculados, out string motivo)
        {
            motivo = string.Empty;

            if (cursosMatriculados.Contains(curso.Id))
            {
                motivo = "Ya matriculado";
                return false;
            }

            var cicloCurso = curso.Ciclo;

            // Permitir cursos del ciclo actual y ciclos anteriores
            if (cicloCurso > cicloActual)
            {
                motivo = $"Requiere ciclo {cicloCurso} (estás en ciclo {cicloActual})";
                return false;
            }

            return true;
        }

        public async Task<List<MatriculaDto>> GetMisCursosAsync(int idEstudiante, int? idPeriodo = null)
        {
            var query = _context.Matriculas
                .Include(m => m.Curso!)
                    .ThenInclude(c => c.Docente)
                .Include(m => m.Periodo)
                .Include(m => m.Notas)
                .Where(m => m.IdEstudiante == idEstudiante);

            if (idPeriodo.HasValue)
            {
                query = query.Where(m => m.IdPeriodo == idPeriodo.Value);
            }

            var matriculas = await query.ToListAsync();

            return matriculas.Select(m => new MatriculaDto
            {
                Id = m.Id,
                IdEstudiante = m.IdEstudiante,
                IdCurso = m.IdCurso,
                CodigoCurso = m.Curso?.Codigo ?? string.Empty,
                NombreCurso = m.Curso?.NombreCurso ?? string.Empty,
                Creditos = m.Curso?.Creditos ?? 0,
                HorasSemanal = m.Curso?.HorasSemanal ?? 0,
                NombreDocente = m.Curso?.Docente != null 
                    ? $"{m.Curso.Docente.Nombres} {m.Curso.Docente.Apellidos}" 
                    : "Sin asignar",
                IdPeriodo = m.IdPeriodo,
                NombrePeriodo = m.Periodo?.Nombre ?? string.Empty,
                FechaMatricula = m.FechaMatricula,
                Estado = m.Estado ?? string.Empty,
                FechaRetiro = m.FechaRetiro,
                // Usar PromedioFinal guardado si existe (período cerrado), sino calcular
                PromedioFinal = m.PromedioFinal ?? CalcularPromedioFinal(m.Notas?.ToList() ?? new List<Nota>())
            }).ToList();
        }

        private decimal? CalcularPromedioFinal(List<Nota> notas)
        {
            if (notas == null || !notas.Any()) return null;

            var pesoTotal = notas.Sum(n => n.Peso);
            if (pesoTotal == 0) return null;

            var notaPonderada = notas.Sum(n => n.NotaValor * n.Peso / 100);
            return Math.Round(notaPonderada, 2);
        }

        public async Task<MatriculaDto> MatricularAsync(int idEstudiante, MatricularDto dto, bool isAutorizado = false)
        {
            // Validar que el periodo existe y está activo
            var periodo = await _context.Periodos.FindAsync(dto.IdPeriodo);
            if (periodo == null)
                throw new Exception("El período no existe");

            if (!periodo.Activo)
                throw new Exception("El período no está activo");

            // Validar que el estudiante existe
            var estudiante = await _context.Estudiantes.FindAsync(idEstudiante);
            if (estudiante == null)
                throw new Exception("El estudiante no existe");

            // Validar que el curso existe
            var curso = await _context.Cursos
                .Include(c => c.Docente)
                .FirstOrDefaultAsync(c => c.Id == dto.IdCurso);

            if (curso == null)
                throw new Exception("El curso no existe");

            // Buscar si existe alguna matrícula previa (cualquier estado)
            var matriculaExistente = await _context.Matriculas
                .FirstOrDefaultAsync(m => 
                    m.IdEstudiante == idEstudiante && 
                    m.IdCurso == dto.IdCurso && 
                    m.IdPeriodo == dto.IdPeriodo);

            // Si ya está matriculado, no permitir duplicado
            if (matriculaExistente != null && matriculaExistente.Estado == "Matriculado")
                throw new Exception("Ya estás matriculado en este curso para el período seleccionado");

            // ============================================================
            // VALIDACIÓN ESPECIAL: Práctica Pre Profesional requiere 140 créditos
            // ============================================================
            if (curso.NombreCurso.ToLower().Contains("práctica") && curso.NombreCurso.ToLower().Contains("profesional") ||
                curso.NombreCurso.ToLower().Contains("practica") && curso.NombreCurso.ToLower().Contains("profesional") ||
                curso.NombreCurso.ToLower().Contains("prácticas") && curso.NombreCurso.ToLower().Contains("profesional"))
            {
                const int CREDITOS_MINIMOS_PRACTICA = 140;
                
                // Calcular créditos aprobados del estudiante
                var creditosAprobados = await _context.Matriculas
                    .Include(m => m.Curso)
                    .Where(m => m.IdEstudiante == idEstudiante &&
                               m.PromedioFinal.HasValue &&
                               m.PromedioFinal.Value >= 11 &&
                               m.Estado != "Retirado")
                    .SumAsync(m => m.Curso != null ? m.Curso.Creditos : 0);

                if (creditosAprobados < CREDITOS_MINIMOS_PRACTICA)
                {
                    throw new Exception($"Para matricularte en '{curso.NombreCurso}' necesitas tener al menos {CREDITOS_MINIMOS_PRACTICA} créditos aprobados. Actualmente tienes {creditosAprobados} créditos aprobados.");
                }
            }

            // Validar prerequisitos solo si NO es autorizado
            // NO validar ciclo aquí porque fn_CursosDisponibles ya filtra correctamente
            // (incluye cursos del ciclo actual Y cursos reprobados de ciclos anteriores)
            if (!isAutorizado)
            {
                // ============================================================
                // NUEVA VALIDACIÓN: Verificar si el curso fue jalado ESTE MISMO AÑO
                // Si jaló en 2025-I, no puede llevarlo hasta 2026-I
                // ============================================================
                var cursoJaladoMismoAnio = await _context.Matriculas
                    .Include(m => m.Periodo)
                    .Where(m => m.IdEstudiante == idEstudiante && 
                               m.IdCurso == dto.IdCurso &&
                               m.PromedioFinal.HasValue &&
                               m.PromedioFinal.Value < 11 &&  // Jalado
                               m.Estado != "Retirado" &&
                               m.Periodo != null &&
                               m.Periodo.Anio == periodo.Anio)  // Mismo año que el período activo
                    .FirstOrDefaultAsync();

                if (cursoJaladoMismoAnio != null)
                {
                    throw new Exception($"No puedes matricularte en '{curso.NombreCurso}' porque lo jalaste en el período {cursoJaladoMismoAnio.Periodo?.Nombre ?? "anterior"}. Debes esperar hasta el próximo año académico ({periodo.Anio + 1}) para volver a llevarlo.");
                }

                // ============================================================
                // NUEVA VALIDACIÓN: Verificar si algún prerequisito fue jalado ESTE MISMO AÑO
                // Los cursos dependientes también quedan bloqueados
                // ============================================================
                var prerequisitosDelCurso = await _context.CursoPrerequisitos
                    .Include(cp => cp.Prerequisito)
                    .Where(cp => cp.IdCurso == dto.IdCurso)
                    .ToListAsync();

                foreach (var prereq in prerequisitosDelCurso)
                {
                    var prereqJaladoMismoAnio = await _context.Matriculas
                        .Include(m => m.Periodo)
                        .Where(m => m.IdEstudiante == idEstudiante && 
                                   m.IdCurso == prereq.IdCursoPrerequisito &&
                                   m.PromedioFinal.HasValue &&
                                   m.PromedioFinal.Value < 11 &&  // Jalado
                                   m.Estado != "Retirado" &&
                                   m.Periodo != null &&
                                   m.Periodo.Anio == periodo.Anio)  // Mismo año
                        .FirstOrDefaultAsync();

                    if (prereqJaladoMismoAnio != null)
                    {
                        throw new Exception($"No puedes matricularte en '{curso.NombreCurso}' porque su prerequisito '{prereq.Prerequisito?.NombreCurso ?? "desconocido"}' fue jalado en el período {prereqJaladoMismoAnio.Periodo?.Nombre ?? "anterior"}. Debes esperar hasta el próximo año académico ({periodo.Anio + 1}).");
                    }
                }

                // Validar prerequisitos del curso
                var prerequisitos = await _context.CursoPrerequisitos
                    .Include(cp => cp.Prerequisito)
                    .Where(cp => cp.IdCurso == dto.IdCurso)
                    .ToListAsync();
                
                if (prerequisitos.Any())
                {
                    // Obtener cursos aprobados por el estudiante (nota >= 11)
                    var cursosAprobados = await _context.Matriculas
                        .Where(m => m.IdEstudiante == idEstudiante && 
                                   m.PromedioFinal.HasValue &&
                                   m.PromedioFinal.Value >= 11)
                        .Select(m => m.IdCurso)
                        .ToListAsync();
                    
                    var prerequisitosFaltantes = new List<string>();
                    
                    foreach (var prereq in prerequisitos)
                    {
                        if (!cursosAprobados.Contains(prereq.IdCursoPrerequisito))
                        {
                            // Verificar el estado del prerequisito
                            var matriculaPrerreq = await _context.Matriculas
                                .Where(m => m.IdEstudiante == idEstudiante && 
                                       m.IdCurso == prereq.IdCursoPrerequisito)
                                .OrderByDescending(m => m.FechaMatricula)
                                .FirstOrDefaultAsync();
                            
                            string mensaje = prereq.Prerequisito?.NombreCurso ?? "Curso desconocido";
                            
                            if (matriculaPrerreq != null && matriculaPrerreq.PromedioFinal.HasValue)
                            {
                                mensaje += $" (Nota: {matriculaPrerreq.PromedioFinal.Value} - Requiere nota mínima de 11)";
                            }
                            else if (matriculaPrerreq != null)
                            {
                                mensaje += " (Curso en progreso)";
                            }
                            else
                            {
                                mensaje += " (No cursado)";
                            }
                            
                            prerequisitosFaltantes.Add(mensaje);
                        }
                    }
                    
                    if (prerequisitosFaltantes.Any())
                    {
                        throw new Exception($"No cumples con los prerequisitos requeridos:\n- {string.Join("\n- ", prerequisitosFaltantes)}");
                    }
                }
            }

            Matricula matriculaFinal;

            // Si existe una matrícula retirada, reactivarla
            if (matriculaExistente != null && matriculaExistente.Estado == "Retirado")
            {
                matriculaExistente.Estado = "Matriculado";
                matriculaExistente.FechaMatricula = DateTime.Now;
                matriculaExistente.FechaRetiro = null;
                matriculaExistente.IsAutorizado = isAutorizado; // Actualizar flag de autorización
                matriculaFinal = matriculaExistente;
            }
            else
            {
                // Crear nueva matrícula
                var nuevaMatricula = new Matricula
                {
                    IdEstudiante = idEstudiante,
                    IdCurso = dto.IdCurso,
                    IdPeriodo = dto.IdPeriodo,
                    FechaMatricula = DateTime.Now,
                    Estado = "Matriculado",
                    IsAutorizado = isAutorizado // Establecer flag de autorización
                };

                _context.Matriculas.Add(nuevaMatricula);
                matriculaFinal = nuevaMatricula;
            }

            await _context.SaveChangesAsync();

            // Crear notificación
            var notificacion = new Notificacion
            {
                Tipo = "academico",
                Accion = "matricula",
                Mensaje = $"Te has matriculado exitosamente en el curso: {curso.NombreCurso}",
                MetadataJson = System.Text.Json.JsonSerializer.Serialize(new 
                { 
                    idCurso = curso.Id, 
                    nombreCurso = curso.NombreCurso,
                    periodo = periodo.Nombre
                }),
                IdUsuario = estudiante.IdUsuario,
                FechaCreacion = DateTime.Now,
                Leida = false
            };
            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();

            return new MatriculaDto
            {
                Id = matriculaFinal.Id,
                IdEstudiante = matriculaFinal.IdEstudiante,
                IdCurso = matriculaFinal.IdCurso,
                CodigoCurso = curso.Codigo,
                NombreCurso = curso.NombreCurso,
                Creditos = curso.Creditos,
                HorasSemanal = curso.HorasSemanal,
                NombreDocente = curso.Docente != null 
                    ? $"{curso.Docente.Nombres} {curso.Docente.Apellidos}" 
                    : "Sin asignar",
                IdPeriodo = matriculaFinal.IdPeriodo,
                NombrePeriodo = periodo.Nombre,
                FechaMatricula = matriculaFinal.FechaMatricula,
                Estado = matriculaFinal.Estado ?? "Matriculado"
            };
        }

        public async Task RetirarAsync(int idMatricula, int idEstudiante)
        {
            var matricula = await _context.Matriculas
                .Include(m => m.Periodo)
                .Include(m => m.Curso)
                .Include(m => m.Estudiante!)
                    .ThenInclude(e => e!.Usuario)
                .FirstOrDefaultAsync(m => m.Id == idMatricula && m.IdEstudiante == idEstudiante);

            if (matricula == null)
                throw new Exception("La matrícula no existe o no te pertenece");

            if (matricula.Estado != "Matriculado")
                throw new Exception("Solo puedes retirarte de cursos con estado 'Matriculado'");

            if (matricula.Periodo != null && !matricula.Periodo.Activo)
                throw new Exception("No puedes retirarte de un período que ya no está activo");

            matricula.Estado = "Retirado";
            matricula.FechaRetiro = DateTime.Now;

            await _context.SaveChangesAsync();

            // Crear notificación
            var notificacion = new Notificacion
            {
                Tipo = "academico",
                Accion = "retiro",
                Mensaje = $"Te has retirado del curso: {matricula.Curso?.NombreCurso ?? "Desconocido"}",
                MetadataJson = System.Text.Json.JsonSerializer.Serialize(new 
                { 
                    idCurso = matricula.IdCurso, 
                    nombreCurso = matricula.Curso?.NombreCurso ?? "Desconocido",
                    periodo = matricula.Periodo?.Nombre ?? "Desconocido"
                }),
                IdUsuario = matricula.Estudiante?.IdUsuario,
                FechaCreacion = DateTime.Now,
                Leida = false
            };
            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();
        }

        public async Task<List<NotaDto>> GetNotasAsync(int idEstudiante, int? idPeriodo = null)
        {
            var query = _context.Notas
                .Include(n => n.Matricula!)
                    .ThenInclude(m => m!.Curso)
                .Include(n => n.Matricula!)
                    .ThenInclude(m => m!.Periodo)
                .Where(n => n.Matricula != null && 
                           n.Matricula.IdEstudiante == idEstudiante &&
                           n.Matricula.Estado != "Retirado" &&  // Excluir cursos retirados
                           n.Matricula.Periodo != null && n.Matricula.Periodo.Activo == true); // SOLO PERIODOS ACTIVOS

            if (idPeriodo.HasValue)
            {
                query = query.Where(n => n.Matricula!.IdPeriodo == idPeriodo.Value);
            }

            var notas = await query.ToListAsync();

            return notas.Select(n => new NotaDto
            {
                Id = n.Id,
                IdMatricula = n.IdMatricula,
                NombreCurso = n.Matricula?.Curso?.NombreCurso ?? string.Empty,
                NombrePeriodo = n.Matricula?.Periodo?.Nombre ?? string.Empty,
                TipoEvaluacion = n.TipoEvaluacion,
                NotaValor = n.NotaValor,
                Peso = n.Peso,
                Fecha = n.Fecha,
                Observaciones = n.Observaciones
            }).ToList();
        }

        public async Task<PeriodoDto?> GetPeriodoActivoAsync()
        {
            var periodo = await _context.Periodos
                .FirstOrDefaultAsync(p => p.Activo);

            if (periodo == null) return null;

            return new PeriodoDto
            {
                Id = periodo.Id,
                Nombre = periodo.Nombre,
                Anio = periodo.Anio,
                Ciclo = periodo.Ciclo,
                FechaInicio = periodo.FechaInicio,
                FechaFin = periodo.FechaFin,
                Activo = periodo.Activo
            };
        }

        public async Task<List<PeriodoDto>> GetPeriodosAsync()
        {
            var periodos = await _context.Periodos
                .OrderByDescending(p => p.Anio)
                .ThenByDescending(p => p.Ciclo)
                .ToListAsync();

            return periodos.Select(p => new PeriodoDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                Anio = p.Anio,
                Ciclo = p.Ciclo,
                FechaInicio = p.FechaInicio,
                FechaFin = p.FechaFin,
                Activo = p.Activo
            }).ToList();
        }

        public async Task<RegistroNotasDto> GetRegistroNotasAsync(int idEstudiante)
        {
            var resultado = new RegistroNotasDto();

            // Obtener periodos cerrados del estudiante
            var periodosCerrados = await _context.Periodos
                .Where(p => (p.FechaFin <= DateTime.Now || p.Activo == false) &&
                           _context.Matriculas.Any(m => m.IdPeriodo == p.Id && m.IdEstudiante == idEstudiante))
                .OrderBy(p => p.Anio)
                .ThenBy(p => p.Ciclo)
                .ToListAsync();

            // Variables para cálculo de promedio acumulado
            decimal sumaNotasPonderadasAcumuladas = 0;
            int creditosAcumuladosTotal = 0;
            
            // Variable para el ciclo académico (avanza 1 por cada período cursado)
            int cicloAcademico = 0;

            foreach (var periodo in periodosCerrados)
            {
                // Incrementar ciclo académico (1, 2, 3, 4... por cada período cursado)
                cicloAcademico++;
                
                var semestreDto = new SemestreRegistroDto
                {
                    IdPeriodo = periodo.Id,
                    Periodo = periodo.Nombre,
                    Anio = periodo.Anio,
                    Ciclo = periodo.Ciclo,  // 'I' o 'II' - Semestre del período
                    CicloAcademico = cicloAcademico,  // 1, 2, 3... - Ciclo académico del estudiante
                    FechaInicio = periodo.FechaInicio,
                    FechaFin = periodo.FechaFin,
                    Estado = periodo.Activo ? "Abierto" : "Cerrado"
                };

                // Obtener matrículas del periodo - EXCLUIR CURSOS RETIRADOS
                var matriculas = await _context.Matriculas
                    .Include(m => m.Curso)
                    .Where(m => m.IdEstudiante == idEstudiante && 
                               m.IdPeriodo == periodo.Id && 
                               m.Estado != "Retirado")  // ✅ Excluir cursos retirados
                    .ToListAsync();

                decimal sumaNotasPonderadasSemestre = 0;
                int creditosSemestre = 0;

                foreach (var matricula in matriculas)
                {
                    if (matricula.Curso == null) continue;

                    // Obtener notas de la matrícula
                    var notas = await _context.Notas
                        .Where(n => n.IdMatricula == matricula.Id)
                        .ToListAsync();

                    // ✅ Saltar si no tiene notas
                    if (!notas.Any()) continue;

                    // Calcular nota final
                    decimal notaFinal;
                    if (matricula.PromedioFinal.HasValue)
                    {
                        notaFinal = matricula.PromedioFinal.Value;
                    }
                    else
                    {
                        notaFinal = notas.Sum(n => n.NotaValor * (n.Peso / 100m));
                    }

                    int notaFinalRedondeada = RedondeoComercial(notaFinal);

                    // Obtener fecha de examen
                    DateTime? fechaExamen = notas
                        .Where(n => n.TipoEvaluacion != null && 
                               (n.TipoEvaluacion.ToLower().Contains("examen") && 
                                n.TipoEvaluacion.ToLower().Contains("final")))
                        .OrderByDescending(n => n.Fecha)
                        .Select(n => (DateTime?)n.Fecha)
                        .FirstOrDefault();

                    if (!fechaExamen.HasValue && notas.Any())
                    {
                        fechaExamen = notas.Max(n => n.Fecha);
                    }

                    // Obtener evaluaciones con detalles
                    var evaluaciones = new List<EvaluacionRegistroDto>();
                    foreach (var nota in notas.OrderBy(n => n.Id))
                    {
                        evaluaciones.Add(new EvaluacionRegistroDto
                        {
                            Nombre = nota.TipoEvaluacion ?? "Sin nombre",
                            Peso = (int)nota.Peso,
                            Nota = nota.NotaValor
                        });
                    }

                    var cursoDto = new CursoRegistroDto
                    {
                        IdMatricula = matricula.Id,
                        IdCurso = matricula.Curso.Id,
                        CodigoCurso = matricula.Curso.Codigo,
                        NombreCurso = matricula.Curso.NombreCurso,
                        Creditos = matricula.Curso.Creditos,
                        HorasSemanal = matricula.Curso.HorasSemanal,
                        FechaExamen = fechaExamen,
                        NotaFinal = notaFinalRedondeada,
                        EstadoCurso = notaFinalRedondeada >= 11 ? "Aprobado" : "Desaprobado",
                        Evaluaciones = evaluaciones
                    };

                    semestreDto.Cursos.Add(cursoDto);

                    // Acumular para promedio semestral
                    sumaNotasPonderadasSemestre += notaFinal * matricula.Curso.Creditos;
                    creditosSemestre += matricula.Curso.Creditos;

                    // Acumular para promedio acumulado
                    sumaNotasPonderadasAcumuladas += notaFinal * matricula.Curso.Creditos;
                    creditosAcumuladosTotal += matricula.Curso.Creditos;
                }

                // Calcular totales del semestre
                semestreDto.Totales = new TotalesSemestreDto
                {
                    TotalCreditos = creditosSemestre,
                    TotalHoras = semestreDto.Cursos.Sum(c => c.HorasSemanal),
                    PromedioSemestral = creditosSemestre > 0 
                        ? Math.Round(sumaNotasPonderadasSemestre / creditosSemestre, 2) 
                        : 0,
                    PromedioAcumulado = creditosAcumuladosTotal > 0 
                        ? Math.Round(sumaNotasPonderadasAcumuladas / creditosAcumuladosTotal, 2) 
                        : 0
                };

                resultado.Semestres.Add(semestreDto);
            }

            return resultado;
        }

        private int RedondeoComercial(decimal valor)
        {
            return (int)Math.Round(valor, MidpointRounding.AwayFromZero);
        }
    }
}
