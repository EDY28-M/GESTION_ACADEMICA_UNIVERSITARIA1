using System.Text.Json;
using API_REST_CURSOSACADEMICOS.Application.Events;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Domain.Events;
using API_REST_CURSOSACADEMICOS.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace API_REST_CURSOSACADEMICOS.Infrastructure.Events.Handlers;

/// <summary>
/// Handler que procesa eventos y crea notificaciones en la base de datos y las envía vía SignalR
/// </summary>
public class NotificacionEventHandler : IEventHandler<EstudianteMatriculadoEvent>
{
    private readonly GestionAcademicaContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificacionEventHandler> _logger;

    public NotificacionEventHandler(
        GestionAcademicaContext context,
        IServiceProvider serviceProvider,
        ILogger<NotificacionEventHandler> logger)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task HandleAsync(EstudianteMatriculadoEvent @event)
    {
        try
        {
            var notificacion = new Notificacion
            {
                Tipo = "academico",
                Accion = "matricula",
                Mensaje = $"Te has matriculado exitosamente en el curso: {@event.NombreCurso}",
                MetadataJson = JsonSerializer.Serialize(new
                {
                    idCurso = @event.IdCurso,
                    nombreCurso = @event.NombreCurso,
                    periodo = @event.NombrePeriodo,
                    idMatricula = @event.IdMatricula
                }),
                IdUsuario = @event.IdUsuario,
                FechaCreacion = DateTime.Now,
                Leida = false
            };

            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();

            // Enviar notificación en tiempo real vía SignalR
            try
            {
                // Resolver IHubContext<NotificationsHub> dinámicamente para evitar dependencia circular
                var hubType = Type.GetType("API_REST_CURSOSACADEMICOS.Hubs.NotificationsHub, API_REST_CURSOSACADEMICOS");
                if (hubType != null)
                {
                    var hubContextType = typeof(IHubContext<>).MakeGenericType(hubType);
                    var hubContext = _serviceProvider.GetService(hubContextType);
                    
                    if (hubContext != null)
                    {
                        var notificationDto = new
                        {
                            id = notificacion.Id,
                            tipo = notificacion.Tipo,
                            accion = notificacion.Accion,
                            mensaje = notificacion.Mensaje,
                            metadata = JsonSerializer.Deserialize<object>(notificacion.MetadataJson ?? "{}"),
                            fechaCreacion = notificacion.FechaCreacion,
                            leida = notificacion.Leida
                        };

                        var clientsProperty = hubContextType.GetProperty("Clients");
                        if (clientsProperty != null)
                        {
                            var clients = clientsProperty.GetValue(hubContext);
                            var userMethod = clients?.GetType().GetMethod("User", new[] { typeof(string) });
                            if (userMethod != null)
                            {
                                var userClient = userMethod.Invoke(clients, new object[] { @event.IdUsuario.ToString() });
                                var sendAsyncMethod = userClient?.GetType().GetMethod("SendAsync", new[] { typeof(string), typeof(object) });
                                if (sendAsyncMethod != null)
                                {
                                    await (Task)sendAsyncMethod.Invoke(userClient, new object[] { "ReceiveNotification", notificationDto })!;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ No se pudo enviar notificación vía SignalR, pero la notificación se guardó en BD");
            }

            _logger.LogInformation("✅ Notificación creada y enviada para matrícula: Estudiante {IdEstudiante}, Curso {IdCurso}",
                @event.IdEstudiante, @event.IdCurso);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al procesar evento EstudianteMatriculadoEvent");
            throw;
        }
    }
}

