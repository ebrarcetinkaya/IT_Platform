using System.Text.Json;
using BtPlatform.Core.Entities;

namespace BtPlatform.Core.Services;

// Toplayıcının JSON çıktısını doğrular. Toplayıcıya güvenmeyiz:
// bozuk kayıt atlanır, aşırı büyük çıktı reddedilir, sınırlar uygulanır.
public static class EventJsonParser
{
    public const int MaxEvents = 50;
    public const int MaxMessageChars = 500;
    public const int MaxJsonChars = 256 * 1024;

    public static EventCollectResult Parse(string? json, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(json))
            return EventCollectResult.Success(new List<CollectedEvent>());   // olay yok: boş çıktı normal
        if (json.Length > MaxJsonChars)
            return EventCollectResult.Fail("Olay çıktısı çok büyük, reddedildi.");

        json = json.Trim().TrimStart('\uFEFF');

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var items = new List<JsonElement>();
            if (root.ValueKind == JsonValueKind.Array) items.AddRange(root.EnumerateArray());
            else if (root.ValueKind == JsonValueKind.Object) items.Add(root);
            else return EventCollectResult.Fail("Olay çıktısı beklenen biçimde değil.");

            var events = new List<CollectedEvent>();
            foreach (var it in items)
            {
                if (events.Count >= MaxEvents) break;
                var ev = ParseOne(it, nowUtc);
                if (ev is not null) events.Add(ev);   // bozuk kayıt atlanır
            }

            return EventCollectResult.Success(events);
        }
        catch (JsonException)
        {
            return EventCollectResult.Fail("Olay çıktısı okunamadı (geçersiz JSON).");
        }
    }

    private static CollectedEvent? ParseOne(JsonElement e, DateTime nowUtc)
    {
        if (e.ValueKind != JsonValueKind.Object) return null;

        var log = Str(e, "log", 20);
        if (log != "System" && log != "Application") return null;

        var level = Str(e, "level", 20).ToLowerInvariant() switch
        {
            "critical" => (PcEventLevel?)PcEventLevel.Critical,
            "error" => PcEventLevel.Error,
            "warning" => PcEventLevel.Warning,
            _ => null    // Information ve bilinmeyenler reddedilir
        };
        if (level is null) return null;

        if (!Int(e, "eventId", out var eventId) || eventId < 0 || eventId > 65535) return null;
        if (!Int(e, "recordId", out var recordId) || recordId < 0) return null;

        var source = Str(e, "source", 100);
        if (source == "") return null;

        var timeText = Str(e, "time", 40);
        if (!DateTime.TryParse(timeText, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                out var time)) return null;
        if (time > nowUtc.AddMinutes(5) || time < nowUtc.AddYears(-1)) return null;

        var message = Str(e, "message", MaxMessageChars * 4)
            .Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Trim();
        if (message.Length > MaxMessageChars) message = message[..MaxMessageChars];

        return new CollectedEvent(log, level.Value, (int)eventId, source, message, time, recordId);
    }

    private static string Str(JsonElement e, string name, int max)
    {
        if (!e.TryGetProperty(name, out var p) || p.ValueKind != JsonValueKind.String) return "";
        var s = (p.GetString() ?? "").Trim();
        return s.Length > max ? s[..max] : s;
    }

    private static bool Int(JsonElement e, string name, out long value)
    {
        value = 0;
        return e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetInt64(out value);
    }
}