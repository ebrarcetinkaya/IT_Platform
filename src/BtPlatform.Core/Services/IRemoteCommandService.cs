using BtPlatform.Core.Entities;

namespace BtPlatform.Core.Services;

// Komut isteği: katalogdan (CatalogKey dolu) ya da serbest (FreeCommand dolu). İkisi birden olmaz.
public record RemoteCommandRequest(int ComputerId, string? CatalogKey, string? Parameter,
    string? FreeCommand, string Reason);

public record RemoteCommandResult(RemoteCommandJob? Job, string? Error)
{
    public bool Ok => Job is not null;
    public static RemoteCommandResult Fail(string error) => new(null, error);
}

public interface IRemoteCommandService
{
    // Kuralları sunucuda yeniden doğrular, uygunsa Pending iş oluşturur. Hiçbir şey çalıştırmaz.
    Task<RemoteCommandResult> CreateAsync(string requestedBy, RemoteCommandRequest request);

    Task<List<RemoteCommandJob>> GetJobsAsync(int computerId, int take = 20);
}