using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Application;
using API_REST_CURSOSACADEMICOS.Infrastructure;
using API_REST_CURSOSACADEMICOS.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// CONFIGURACIÓN DE PUERTO
// ========================================
// Solo configurar UseUrls si hay variable PORT (producción/Cloud Run)
// En desarrollo, usar el puerto de launchSettings.json (5251)
var portEnv = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(portEnv))
{
    // Producción: usar la variable PORT de Cloud Run
    builder.WebHost.UseUrls($"http://0.0.0.0:{portEnv}");
}
// En desarrollo, no usar UseUrls para que respete launchSettings.json

// Logging inicial para debugging
Console.WriteLine($"[INICIO] ASPNETCORE_ENVIRONMENT: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
Console.WriteLine($"[INICIO] PORT (env): {portEnv ?? "No configurado - usando launchSettings.json"}");

// Add services to the container.

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Configurar Health Checks para Load Balancer
// Configurar health check de DB con timeout corto para no bloquear el startup
builder.Services.AddHealthChecks()
    .AddDbContextCheck<GestionAcademicaContext>("database", 
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
        tags: new[] { "db", "sqlserver" })
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API is running"));

// Configurar JWT Authentication
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"];
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "GestionAcademicaAPI";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "GestionAcademicaClients";

// Validar que JWT SecretKey esté configurado (es el único requerido)
if (string.IsNullOrWhiteSpace(jwtSecretKey))
{
    var errorMsg = "[ERROR CRÍTICO] JWT SecretKey no está configurada.\n" +
                   "Configura JwtSettings__SecretKey como variable de entorno o secret en GitHub Actions.\n" +
                   "Revisa la configuración en GitHub: Settings → Secrets and variables → Actions";
    Console.WriteLine(errorMsg);
    throw new InvalidOperationException("JWT SecretKey no está configurada");
}

Console.WriteLine($"[CONFIG] JWT SecretKey configurada (longitud: {jwtSecretKey.Length} caracteres)");
Console.WriteLine($"[CONFIG] JWT Issuer: {jwtIssuer}");
Console.WriteLine($"[CONFIG] JWT Audience: {jwtAudience}");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero // Eliminar tolerancia de tiempo por defecto (5 minutos)
    };

    // Configurar eventos para logging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var userName = context.Principal?.Identity?.Name ?? "Unknown";
            Console.WriteLine($"Token validated for user: {userName}");
            return Task.CompletedTask;
        },
        // Configurar autenticación para SignalR
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Configurar CORS para permitir acceso desde cualquier red
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Permite cualquier origen
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Permite credenciales
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configurar serialización JSON en camelCase para compatibilidad con frontend
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Configurar SignalR para notificaciones en tiempo real
builder.Services.AddSignalR();

// Configurar Swagger con soporte JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gestión Académica API",
        Version = "v1",
        Description = "API REST para gestión académica con autenticación JWT"
    });

    // Configurar autenticación JWT en Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT en el formato: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Logging después de construir la app
Console.WriteLine($"[APP] Aplicación construida correctamente");
Console.WriteLine($"[APP] Ambiente: {app.Environment.EnvironmentName}");
Console.WriteLine($"[APP] URLs configuradas: {string.Join(", ", app.Urls)}");

// Configure the HTTP request pipeline.
// ========================================
// CONFIGURACIÓN PARA LOAD BALANCER
// ========================================
// Configurar headers para proxy/load balancer (DEBE IR PRIMERO)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                      Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Gestión Académica API v1");
});

// app.UseHttpsRedirection(); // Deshabilitado para contenedor Docker

// Usar CORS (debe ir antes de Authentication y Authorization)
app.UseCors("AllowAll");

// Middleware de autenticación y autorización (orden importante)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Mapear el hub de SignalR
app.MapHub<NotificationsHub>("/hub/notifications");

// Health checks para load balancer
// Nota: Cloud Run verifica que el puerto esté escuchando, no este endpoint específico
app.UseHealthChecks("/health", new HealthCheckOptions
{
    // Permitir que el health check devuelva 200 incluso si algunos checks están degradados
    ResultStatusCodes =
    {
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy] = StatusCodes.Status200OK,
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded] = StatusCodes.Status200OK,
        [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    },
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            instance = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "Unknown",
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(x => new { 
                name = x.Key, 
                status = x.Value.Status.ToString(),
                duration = x.Value.Duration.TotalMilliseconds,
                description = x.Value.Description
            })
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});

// Endpoint simple para load balancer health check
app.MapGet("/health/simple", () => Results.Ok("healthy"));

// Información de la instancia para debugging
app.MapGet("/info", () => Results.Ok(new { 
    instance = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "Unknown",
    machineName = Environment.MachineName,
    processId = Environment.ProcessId,
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}));

Console.WriteLine($"[LISTO] Aplicación iniciando en: {string.Join(", ", app.Urls)}");
Console.WriteLine($"[LISTO] Health check disponible en: /health");
Console.WriteLine($"[LISTO] Swagger disponible en: /swagger");

app.Run();
