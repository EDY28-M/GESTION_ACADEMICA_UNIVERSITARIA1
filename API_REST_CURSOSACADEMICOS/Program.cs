using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using API_REST_CURSOSACADEMICOS.Data;
using API_REST_CURSOSACADEMICOS.Application;
using API_REST_CURSOSACADEMICOS.Infrastructure;
using API_REST_CURSOSACADEMICOS.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// CONFIGURACIÓN DE PUERTO PARA CLOUD RUN
// ========================================
// Cloud Run inyecta la variable PORT automáticamente
// Leemos el puerto y configuramos la URL antes de construir la app
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Logging inicial para debugging
Console.WriteLine($"[INICIO] Puerto configurado: {port}");
Console.WriteLine($"[INICIO] ASPNETCORE_ENVIRONMENT: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
Console.WriteLine($"[INICIO] PORT (env): {Environment.GetEnvironmentVariable("PORT")}");

// Add services to the container.

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Configurar Health Checks para Load Balancer
builder.Services.AddHealthChecks()
    .AddDbContextCheck<GestionAcademicaContext>("database")
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API is running"));

// Configurar JWT Authentication
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecretKey))
{
    var errorMsg = "JWT SecretKey no configurada. Configura JwtSettings__SecretKey como variable de entorno.";
    Console.WriteLine($"[ERROR] {errorMsg}");
    throw new InvalidOperationException(errorMsg);
}
Console.WriteLine($"[CONFIG] JWT SecretKey configurada (longitud: {jwtSecretKey.Length} caracteres)");

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
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
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
            Console.WriteLine($"Token validated for user: {context.Principal?.Identity?.Name}");
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

builder.Services.AddControllers();

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
// Configure the HTTP request pipeline.
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

// ========================================
// CONFIGURACIÓN PARA LOAD BALANCER
// ========================================
// Configurar headers para proxy/load balancer
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                      Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

// Health checks para load balancer
app.UseHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var response = new
        {
            status = report.Status.ToString(),
            instance = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "Unknown",
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(x => new { 
                name = x.Key, 
                status = x.Value.Status.ToString(),
                duration = x.Value.Duration.TotalMilliseconds 
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

Console.WriteLine($"[LISTO] Aplicación iniciando en puerto {port}");
Console.WriteLine($"[LISTO] Health check disponible en: /health");
Console.WriteLine($"[LISTO] Swagger disponible en: /swagger");

app.Run();
