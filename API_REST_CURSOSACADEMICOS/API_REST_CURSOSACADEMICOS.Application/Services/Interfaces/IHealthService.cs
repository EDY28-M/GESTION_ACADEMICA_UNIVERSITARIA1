namespace API_REST_CURSOSACADEMICOS.Services.Interfaces;

public record HealthDbInfo(bool CanConnect, int? UserCount, string? ConnectionStringPreview);

public interface IHealthService
{
    Task<bool> CanConnectDbAsync();
    Task<HealthDbInfo> GetDatabaseInfoAsync();
}


