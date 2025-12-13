using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.DTOs;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API_REST_CURSOSACADEMICOS.Services;

public sealed class EstudiantesControllerService : IEstudiantesControllerService
{
    private readonly GestionAcademicaContext _context;
    private readonly IEstudianteService _estudianteService;

    public EstudiantesControllerService(GestionAcademicaContext context, IEstudianteService estudianteService)
    {
        _context = context;
        _estudianteService = estudianteService;
    }

    public async Task<List<EstudianteDto>> GetAllAdminAsync()
    {
        return await _context.Estudiantes
            .Select(e => new EstudianteDto
            {
                Id = e.Id,
                Codigo = e.Codigo,
                Nombres = e.Nombres,
                Apellidos = e.Apellidos,
                Dni = e.Dni,
                Correo = e.Correo,
                Telefono = e.Telefono,
                Direccion = e.Direccion,
                CicloActual = e.CicloActual,
                CreditosAcumulados = e.CreditosAcumulados,
                PromedioAcumulado = e.PromedioAcumulado,
                PromedioSemestral = e.PromedioSemestral,
                Estado = e.Estado,
                Carrera = e.Carrera
            })
            .OrderBy(e => e.Apellidos)
            .ThenBy(e => e.Nombres)
            .ToListAsync();
    }

    public async Task<EstudianteDto?> GetByIdAdminAsync(int id)
    {
        return await _context.Estudiantes
            .Where(e => e.Id == id)
            .Select(e => new EstudianteDto
            {
                Id = e.Id,
                Codigo = e.Codigo,
                Nombres = e.Nombres,
                Apellidos = e.Apellidos,
                Dni = e.Dni,
                Correo = e.Correo,
                Telefono = e.Telefono,
                Direccion = e.Direccion,
                CicloActual = e.CicloActual,
                CreditosAcumulados = e.CreditosAcumulados,
                PromedioAcumulado = e.PromedioAcumulado,
                PromedioSemestral = e.PromedioSemestral,
                Estado = e.Estado,
                Carrera = e.Carrera
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<int>> GetCursosMatriculadosIdsAsync(int idEstudiante, int idPeriodo)
    {
        return await _context.Matriculas
            .Where(m => m.IdEstudiante == idEstudiante &&
                        m.IdPeriodo == idPeriodo &&
                        m.Estado == "Matriculado")
            .Select(m => m.IdCurso)
            .ToListAsync();
    }

    public async Task<object> VerificarPrerequisitosAsync(int idEstudiante, int idCurso)
    {
        var prerequisitos = await _context.CursoPrerequisitos
            .Include(cp => cp.Prerequisito)
            .Where(cp => cp.IdCurso == idCurso)
            .ToListAsync();

        if (!prerequisitos.Any())
        {
            return new
            {
                cumplePrerequisitos = true,
                mensaje = "Este curso no tiene prerequisitos",
                prerequisitosFaltantes = new List<object>()
            };
        }

        var cursosAprobados = await _context.Matriculas
            .Where(m => m.IdEstudiante == idEstudiante &&
                       m.PromedioFinal.HasValue &&
                       m.PromedioFinal.Value >= 11)
            .Select(m => m.IdCurso)
            .ToListAsync();

        var prerequisitosFaltantes = new List<object>();
        bool cumpleTodos = true;

        foreach (var prereq in prerequisitos)
        {
            var aprobado = cursosAprobados.Contains(prereq.IdCursoPrerequisito);
            if (!aprobado)
            {
                cumpleTodos = false;

                var matriculaCurso = await _context.Matriculas
                    .Where(m => m.IdEstudiante == idEstudiante &&
                               m.IdCurso == prereq.IdCursoPrerequisito)
                    .OrderByDescending(m => m.FechaMatricula)
                    .FirstOrDefaultAsync();

                string estado = "No cursado";
                decimal? nota = null;

                if (matriculaCurso != null && matriculaCurso.PromedioFinal.HasValue)
                {
                    nota = matriculaCurso.PromedioFinal.Value;
                    estado = nota >= 11 ? "Aprobado" : $"Reprobado (Nota: {nota})";
                }
                else if (matriculaCurso != null)
                {
                    estado = "Cursando";
                }

                prerequisitosFaltantes.Add(new
                {
                    id = prereq.Prerequisito.Id,
                    codigo = prereq.Prerequisito.Codigo,
                    nombre = prereq.Prerequisito.NombreCurso,
                    ciclo = prereq.Prerequisito.Ciclo,
                    estado,
                    nota
                });
            }
        }

        return new
        {
            cumplePrerequisitos = cumpleTodos,
            mensaje = cumpleTodos
                ? "Cumples con todos los prerequisitos"
                : "Te faltan aprobar algunos prerequisitos",
            prerequisitosFaltantes
        };
    }

    public async Task<List<OrdenMeritoDto>> GetOrdenMeritoAsync(string? promocion)
    {
        var query = promocion == null
            ? "SELECT * FROM vw_OrdenMerito ORDER BY promocion DESC, posicion ASC"
            : $"SELECT * FROM vw_OrdenMerito WHERE promocion = '{promocion}' ORDER BY posicion ASC";

        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            var command = connection.CreateCommand();
            command.CommandText = query;

            var ordenMerito = new List<OrdenMeritoDto>();

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    ordenMerito.Add(new OrdenMeritoDto
                    {
                        Posicion = Convert.ToInt32(reader.GetInt64(reader.GetOrdinal("posicion"))),
                        Codigo = reader.GetString(reader.GetOrdinal("codigo")),
                        Nombres = reader.GetString(reader.GetOrdinal("nombres")),
                        Apellidos = reader.GetString(reader.GetOrdinal("apellidos")),
                        Promocion = reader.IsDBNull(reader.GetOrdinal("promocion")) ? "" : reader.GetString(reader.GetOrdinal("promocion")),
                        Semestre = reader.GetInt32(reader.GetOrdinal("ciclo_actual")),
                        CreditosLlevadosSemestre = reader.GetInt32(reader.GetOrdinal("creditos_llevados_semestre")),
                        CreditosAprobadosSemestre = reader.GetInt32(reader.GetOrdinal("creditos_aprobados_semestre")),
                        TotalCreditosLlevados = reader.GetInt32(reader.GetOrdinal("total_creditos_llevados")),
                        TotalCreditosAprobados = reader.GetInt32(reader.GetOrdinal("total_creditos_aprobados")),
                        PromedioPonderadoSemestral = reader.IsDBNull(reader.GetOrdinal("promedio_ponderado_semestral")) ? 0 : reader.GetDecimal(reader.GetOrdinal("promedio_ponderado_semestral")),
                        PromedioPonderadoAcumulado = reader.IsDBNull(reader.GetOrdinal("promedio_ponderado_acumulado")) ? 0 : reader.GetDecimal(reader.GetOrdinal("promedio_ponderado_acumulado")),
                        RangoMerito = reader.IsDBNull(reader.GetOrdinal("rango_merito")) ? "" : reader.GetString(reader.GetOrdinal("rango_merito")),
                        TotalEstudiantes = reader.GetInt32(reader.GetOrdinal("total_estudiantes")),
                        PeriodoNombre = reader.IsDBNull(reader.GetOrdinal("periodo_nombre")) ? null : reader.GetString(reader.GetOrdinal("periodo_nombre")),
                        EstadoPeriodo = reader.IsDBNull(reader.GetOrdinal("estado_periodo")) ? null : reader.GetString(reader.GetOrdinal("estado_periodo"))
                    });
                }
            }

            return ordenMerito;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public async Task<List<string>> GetPromocionesAsync()
    {
        return await _context.Estudiantes
            .Where(e => e.Estado == "Activo" && e.Promocion != null && e.Promocion != "")
            .Select(e => e.Promocion!)
            .Distinct()
            .OrderByDescending(p => p)
            .ToListAsync();
    }

    public async Task<ServiceOutcome> GetMiPosicionMeritoAsync(int usuarioId)
    {
        var estudiante = await _context.Estudiantes.FirstOrDefaultAsync(e => e.IdUsuario == usuarioId);
        if (estudiante == null)
        {
            return ServiceOutcome.NotFound(new { mensaje = "Estudiante no encontrado" });
        }

        if (string.IsNullOrEmpty(estudiante.Promocion))
        {
            return ServiceOutcome.NotFound(new
            {
                mensaje = "Tu promoción no ha sido registrada. Contacta con administración.",
                codigo = estudiante.Codigo,
                requiereActualizacion = true
            });
        }

        // La vista vw_OrdenMerito requiere SQL real; en InMemory puede fallar y se manejará en el controller como 500.
        var query = $"SELECT * FROM vw_OrdenMerito WHERE codigo = '{estudiante.Codigo}'";

        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            var command = connection.CreateCommand();
            command.CommandText = query;

            OrdenMeritoDto? miPosicion = null;

            using (var reader = await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    miPosicion = new OrdenMeritoDto
                    {
                        Posicion = Convert.ToInt32(reader.GetInt64(reader.GetOrdinal("posicion"))),
                        Codigo = reader.GetString(reader.GetOrdinal("codigo")),
                        Nombres = reader.GetString(reader.GetOrdinal("nombres")),
                        Apellidos = reader.GetString(reader.GetOrdinal("apellidos")),
                        Promocion = reader.IsDBNull(reader.GetOrdinal("promocion")) ? "" : reader.GetString(reader.GetOrdinal("promocion")),
                        Semestre = reader.GetInt32(reader.GetOrdinal("ciclo_actual")),
                        CreditosLlevadosSemestre = reader.GetInt32(reader.GetOrdinal("creditos_llevados_semestre")),
                        CreditosAprobadosSemestre = reader.GetInt32(reader.GetOrdinal("creditos_aprobados_semestre")),
                        TotalCreditosLlevados = reader.GetInt32(reader.GetOrdinal("total_creditos_llevados")),
                        TotalCreditosAprobados = reader.GetInt32(reader.GetOrdinal("total_creditos_aprobados")),
                        PromedioPonderadoSemestral = reader.IsDBNull(reader.GetOrdinal("promedio_ponderado_semestral")) ? 0 : reader.GetDecimal(reader.GetOrdinal("promedio_ponderado_semestral")),
                        PromedioPonderadoAcumulado = reader.IsDBNull(reader.GetOrdinal("promedio_ponderado_acumulado")) ? 0 : reader.GetDecimal(reader.GetOrdinal("promedio_ponderado_acumulado")),
                        RangoMerito = reader.IsDBNull(reader.GetOrdinal("rango_merito")) ? "" : reader.GetString(reader.GetOrdinal("rango_merito")),
                        TotalEstudiantes = reader.GetInt32(reader.GetOrdinal("total_estudiantes")),
                        PeriodoNombre = reader.IsDBNull(reader.GetOrdinal("periodo_nombre")) ? null : reader.GetString(reader.GetOrdinal("periodo_nombre")),
                        EstadoPeriodo = reader.IsDBNull(reader.GetOrdinal("estado_periodo")) ? null : reader.GetString(reader.GetOrdinal("estado_periodo"))
                    };
                }
            }

            if (miPosicion == null)
            {
                return ServiceOutcome.NotFound(new
                {
                    mensaje = "No apareces en el orden de mérito. Verifica que tu estado sea 'Activo' y tengas promoción asignada.",
                    codigo = estudiante.Codigo,
                    promocion = estudiante.Promocion,
                    estado = estudiante.Estado
                });
            }

            return ServiceOutcome.Ok(miPosicion);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public async Task<ServiceOutcome> CambiarContrasenaAsync(int usuarioId, CambiarContrasenaDto request)
    {
        var usuario = await _context.Usuarios.FindAsync(usuarioId);
        if (usuario == null)
            return ServiceOutcome.NotFound(new { mensaje = "Usuario no encontrado" });

        if (!BCrypt.Net.BCrypt.Verify(request.ContrasenaActual, usuario.PasswordHash))
            return ServiceOutcome.BadRequest(new { mensaje = "La contraseña actual es incorrecta" });

        if (string.IsNullOrWhiteSpace(request.ContrasenaNueva) || request.ContrasenaNueva.Length < 6)
            return ServiceOutcome.BadRequest(new { mensaje = "La nueva contraseña debe tener al menos 6 caracteres" });

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.ContrasenaNueva);
        await _context.SaveChangesAsync();

        return ServiceOutcome.Ok(new { mensaje = "Contraseña actualizada exitosamente" });
    }

    public async Task<ServiceOutcome> ActualizarPerfilAsync(int usuarioId, ActualizarPerfilDto request)
    {
        var estudiante = await _context.Estudiantes
            .Include(e => e.Usuario)
            .FirstOrDefaultAsync(e => e.IdUsuario == usuarioId);

        if (estudiante == null)
            return ServiceOutcome.NotFound(new { mensaje = "Estudiante no encontrado" });

        estudiante.Apellidos = request.Apellidos;
        estudiante.Nombres = request.Nombres;
        estudiante.Dni = request.Dni;
        estudiante.FechaNacimiento = request.FechaNacimiento;
        estudiante.Correo = request.Correo;
        estudiante.Telefono = request.Telefono;
        estudiante.Direccion = request.Direccion;

        await _context.SaveChangesAsync();

        var perfilActualizado = await _estudianteService.GetByUsuarioIdAsync(usuarioId);
        return ServiceOutcome.Ok(new { mensaje = "Perfil actualizado exitosamente", estudiante = perfilActualizado });
    }
}


