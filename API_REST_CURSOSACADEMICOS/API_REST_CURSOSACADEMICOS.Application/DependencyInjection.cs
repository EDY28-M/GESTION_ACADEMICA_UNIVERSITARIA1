using Microsoft.Extensions.DependencyInjection;

namespace API_REST_CURSOSACADEMICOS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Punto único para registrar cross-cutting concerns (validación, MediatR, etc.).
        // Por ahora no hay componentes propios de Application para registrar.
        return services;
    }
}


