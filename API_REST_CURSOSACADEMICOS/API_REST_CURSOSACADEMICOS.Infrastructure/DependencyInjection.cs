using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Services;
using API_REST_CURSOSACADEMICOS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API_REST_CURSOSACADEMICOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GestionAcademicaContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Servicios de aplicación (interfaces en Application, implementaciones aquí)
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEstudianteService, EstudianteService>();
        services.AddScoped<IAsistenciaService, AsistenciaService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IHorarioService, HorarioService>();
        services.AddScoped<ICursosService, CursosService>();
        services.AddScoped<INotificacionesService, NotificacionesService>();
        services.AddScoped<IUserLookupService, UserLookupService>();
        services.AddScoped<IDocentesService, DocentesService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IHealthService, HealthService>();
        services.AddScoped<IEstudiantesControllerService, EstudiantesControllerService>();

        services.AddScoped<EmailService>();

        return services;
    }
}


