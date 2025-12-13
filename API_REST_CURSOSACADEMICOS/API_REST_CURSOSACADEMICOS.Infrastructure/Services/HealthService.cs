using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API_REST_CURSOSACADEMICOS.Services;

public sealed class HealthService : IHealthService
{
    private readonly GestionAcademicaContext _context;

    public HealthService(GestionAcademicaContext context)
    {
        _context = context;
    }

    public async Task<bool> CanConnectDbAsync()
    {
        return await _context.Database.CanConnectAsync();
    }

    public async Task<HealthDbInfo> GetDatabaseInfoAsync()
    {
        var canConnect = await _context.Database.CanConnectAsync();
        if (!canConnect)
        {
            return new HealthDbInfo(false, null, null);
        }

        var userCount = await _context.Usuarios.CountAsync();

        var cs = _context.Database.GetConnectionString();
        string? preview = null;
        if (!string.IsNullOrWhiteSpace(cs))
        {
            preview = cs.Length <= 50 ? cs : cs.Substring(0, 50) + "...";
        }

        return new HealthDbInfo(true, userCount, preview);
    }
}


