using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Models;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API_REST_CURSOSACADEMICOS.Services;

public class UserLookupService : IUserLookupService
{
    private readonly GestionAcademicaContext _context;

    public UserLookupService(GestionAcademicaContext context)
    {
        _context = context;
    }

    public async Task<int?> GetDocenteIdByEmailAsync(string email)
    {
        var docente = await _context.Docentes.AsNoTracking().FirstOrDefaultAsync(d => d.Correo == email);
        return docente?.Id;
    }

    public async Task<Docente?> GetDocenteByEmailAsync(string email)
    {
        return await _context.Docentes.AsNoTracking().FirstOrDefaultAsync(d => d.Correo == email);
    }

    public async Task<int?> GetEstudianteIdByUsuarioIdAsync(int usuarioId)
    {
        var estudiante = await _context.Estudiantes.AsNoTracking().FirstOrDefaultAsync(e => e.IdUsuario == usuarioId);
        return estudiante?.Id;
    }

    public async Task<int?> GetEstudianteIdByEmailAsync(string email)
    {
        var estudiante = await _context.Estudiantes.AsNoTracking().FirstOrDefaultAsync(e => e.Correo == email);
        return estudiante?.Id;
    }
}


