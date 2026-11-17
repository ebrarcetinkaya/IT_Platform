using Microsoft.AspNetCore.Components.Authorization;

namespace BtPlatform.Web.Services;

public record CurrentUser(string Username, string DisplayName, bool IsOperator, bool IsAdmin = false);

// Sunucu tarafı yetki kontrolü: butonu gizlemek yetmez, işlemi yapan kod da kontrol etmeli.
public class AccessGuard
{
    private readonly AuthenticationStateProvider _auth;
    private readonly AuditLogger _audit;

    public AccessGuard(AuthenticationStateProvider auth, AuditLogger audit)
    {
        _auth = auth;
        _audit = audit;
    }

    // Giriş yapmış kullanıcının bilgisi. Giriş yoksa null.
    public async Task<CurrentUser?> GetCurrentUserAsync()
    {
        var user = (await _auth.GetAuthenticationStateAsync()).User;
        if (user.Identity?.IsAuthenticated != true) return null;

        var name = user.Identity.Name ?? "";
        var display = user.FindFirst("display_name")?.Value ?? name;
        var isAdmin = user.IsInRole("Admin");
        var isOperator = isAdmin || user.IsInRole("Technician");
        return new CurrentUser(name, display, isOperator, isAdmin);
    }

    // Admin veya Teknisyen ise kullanıcı adını döner. Değilse null döner ve denemeyi audit log'a yazar.
    public Task<string?> RequireOperatorAsync(string action) =>
        RequireAsync(u => u.IsInRole("Admin") || u.IsInRole("Technician"), action);

    // Sadece Admin
    public Task<string?> RequireAdminAsync(string action) =>
        RequireAsync(u => u.IsInRole("Admin"), action);

    private async Task<string?> RequireAsync(Func<System.Security.Claims.ClaimsPrincipal, bool> allowed, string action)
    {
        var user = (await _auth.GetAuthenticationStateAsync()).User;

        if (user.Identity?.IsAuthenticated == true && allowed(user))
            return user.Identity.Name;

        await _audit.LogAsync(user.Identity?.Name ?? "anonim", "Yetkisiz deneme", action);
        return null;
    }
}