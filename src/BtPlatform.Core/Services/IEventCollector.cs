using BtPlatform.Core.Entities;

namespace BtPlatform.Core.Services;

// Toplayıcının okuduğu ham olay (veritabanı kaydı değil)
public record CollectedEvent(string LogName, PcEventLevel Level, int EventId, string Source,
    string Message, DateTime OccurredAtUtc, long RecordId);

public record EventCollectResult(IReadOnlyList<CollectedEvent> Events, string? Error)
{
    public bool Ok => Error is null;
    public static EventCollectResult Success(IReadOnlyList<CollectedEvent> e) => new(e, null);
    public static EventCollectResult Fail(string error) => new(new List<CollectedEvent>(), error);
}

public interface IEventCollector
{
    // Çalıştığı bilgisayarın System ve Application günlüklerinden son hata/uyarıları SALT OKUNUR toplar.
    Task<EventCollectResult> CollectAsync(CancellationToken ct = default);
}