using System.Collections.Concurrent;

namespace BtPlatform.Web.Services;

// Art arda yanlış parola girilirse o kullanıcı adını geçici kilitler.
// Bellekte tutulur: uygulama yeniden başlayınca sıfırlanır (demo için yeterli).
public class LoginThrottle
{
    private const int MaxFailures = 5;
    private static readonly TimeSpan LockTime = TimeSpan.FromMinutes(5);

    private record State(int Failures, DateTime? LockedUntil);
    private readonly ConcurrentDictionary<string, State> _state = new();

    public bool IsLocked(string username)
    {
        var key = Key(username);
        if (!_state.TryGetValue(key, out var s) || s.LockedUntil is null) return false;
        if (s.LockedUntil > DateTime.UtcNow) return true;
        _state.TryRemove(key, out _);   // kilit süresi bitti
        return false;
    }

    public void Fail(string username) =>
        _state.AddOrUpdate(Key(username),
            _ => new State(1, null),
            (_, s) =>
            {
                var f = s.Failures + 1;
                return f >= MaxFailures
                    ? new State(0, DateTime.UtcNow.Add(LockTime))
                    : new State(f, null);
            });

    public void Reset(string username) => _state.TryRemove(Key(username), out _);

    private static string Key(string username) => username.Trim().ToLowerInvariant();
}