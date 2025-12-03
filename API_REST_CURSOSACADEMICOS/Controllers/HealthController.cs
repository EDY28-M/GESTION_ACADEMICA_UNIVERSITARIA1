using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using API_REST_CURSOSACADEMICOS.Data;

namespace API_REST_CURSOSACADEMICOS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly GestionAcademicaContext _context;
        private readonly ILogger<HealthController> _logger;

        public HealthController(GestionAcademicaContext context, ILogger<HealthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Health check endpoint para load balancer
        /// </summary>
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var healthStatus = new
                {
                    status = "Healthy",
                    timestamp = DateTime.UtcNow,
                    instance = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "Unknown",
                    machineName = Environment.MachineName,
                    processId = Environment.ProcessId,
                    uptime = DateTime.UtcNow.Subtract(Process.GetCurrentProcess().StartTime),
                    version = "1.0.0"
                };

                _logger.LogInformation("Health check OK from instance: {Instance}", healthStatus.instance);
                return Ok(healthStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(503, new { status = "Unhealthy", error = ex.Message });
            }
        }

        /// <summary>
        /// Detailed health check including database connectivity
        /// </summary>
        [HttpGet("detailed")]
        public async Task<IActionResult> GetDetailed()
        {
            var healthChecks = new Dictionary<string, object>();
            var overallStatus = "Healthy";

            try
            {
                // Basic system info
                healthChecks["system"] = new
                {
                    status = "Healthy",
                    instance = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "Unknown",
                    machineName = Environment.MachineName,
                    processId = Environment.ProcessId,
                    workingSet = GC.GetTotalMemory(false),
                    timestamp = DateTime.UtcNow
                };

                // Database connectivity check
                try
                {
                    var dbCanConnect = await _context.Database.CanConnectAsync();
                    if (dbCanConnect)
                    {
                        var userCount = await _context.Usuarios.CountAsync();
                        healthChecks["database"] = new
                        {
                            status = "Healthy",
                            canConnect = true,
                            userCount = userCount,
                            connectionString = _context.Database.GetConnectionString()?.Substring(0, 50) + "..."
                        };
                    }
                    else
                    {
                        healthChecks["database"] = new { status = "Unhealthy", canConnect = false };
                        overallStatus = "Degraded";
                    }
                }
                catch (Exception dbEx)
                {
                    healthChecks["database"] = new 
                    { 
                        status = "Unhealthy", 
                        error = dbEx.Message,
                        canConnect = false 
                    };
                    overallStatus = "Unhealthy";
                }

                // Memory usage
                var process = Process.GetCurrentProcess();
                healthChecks["memory"] = new
                {
                    status = "Healthy",
                    workingSet = process.WorkingSet64 / 1024 / 1024, // MB
                    privateMemory = process.PrivateMemorySize64 / 1024 / 1024, // MB
                    gcMemory = GC.GetTotalMemory(false) / 1024 / 1024 // MB
                };

                // Response time test
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                await Task.Delay(1); // Simulate some work
                stopwatch.Stop();

                healthChecks["performance"] = new
                {
                    status = "Healthy",
                    responseTimeMs = stopwatch.ElapsedMilliseconds,
                    uptime = DateTime.UtcNow.Subtract(process.StartTime)
                };

                var response = new
                {
                    status = overallStatus,
                    timestamp = DateTime.UtcNow,
                    checks = healthChecks
                };

                return overallStatus == "Healthy" ? Ok(response) : StatusCode(503, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detailed health check failed");
                return StatusCode(503, new { 
                    status = "Unhealthy", 
                    error = ex.Message,
                    timestamp = DateTime.UtcNow 
                });
            }
        }

        /// <summary>
        /// Readiness probe para Kubernetes/Docker
        /// </summary>
        [HttpGet("ready")]
        public async Task<IActionResult> Ready()
        {
            try
            {
                // Check if database is accessible
                var canConnect = await _context.Database.CanConnectAsync();
                
                if (canConnect)
                {
                    return Ok(new { 
                        status = "Ready", 
                        timestamp = DateTime.UtcNow,
                        instance = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "Unknown"
                    });
                }
                else
                {
                    return StatusCode(503, new { 
                        status = "NotReady", 
                        reason = "Database not accessible",
                        timestamp = DateTime.UtcNow 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Readiness check failed");
                return StatusCode(503, new { 
                    status = "NotReady", 
                    error = ex.Message,
                    timestamp = DateTime.UtcNow 
                });
            }
        }

        /// <summary>
        /// Liveness probe para Kubernetes/Docker
        /// </summary>
        [HttpGet("live")]
        public IActionResult Live()
        {
            return Ok(new { 
                status = "Alive", 
                timestamp = DateTime.UtcNow,
                instance = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "Unknown",
                processId = Environment.ProcessId
            });
        }
    }
}