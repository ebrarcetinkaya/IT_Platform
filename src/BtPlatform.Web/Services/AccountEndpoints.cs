using System.Security.Claims;
using BtPlatform.Core.Security;
using BtPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BtPlatform.Web.Services;

public static class AccountEndpoints
{
    // Kullanıcı bulunamasa bile aynı süreyi harcamak için sahte hash (timing saldırısına karşı).
    private static readonly string DummyHash = PasswordHasher.Hash("sahte-parola-zamanlama");

    public static void MapAccountEndpoints(this WebApplication app)
    {
        app.MapPost("/account/login", async (
            HttpContext http,
            AppDbContext db,
            AuditLogger audit,
            LoginThrottle throttle,
            [FromForm] string? username,
            [FromForm] string? password,
            [FromForm] string? returnUrl) =>
        {
            username = (username ?? "").Trim();
            password ??= "";
            var target = SafeLocalUrl(returnUrl);
            var shortName = username.Length > 50 ? username[..50] : username;

            string Fail(int code) =>
                $"/login?error={code}&returnUrl={Uri.EscapeDataString(target)}";

            // 1) Kilitli mi?
            if (throttle.IsLocked(username))
            {
                await audit.LogAsync(shortName, "Giriş engellendi", "Çok sayıda başarısız deneme, hesap geçici kilitli");
                return Results.Redirect(Fail(2));
            }

            // 2) Kullanıcıyı bul ve parolayı doğrula
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
            var usable = user is { IsActive: true };
            var hash = usable ? user!.PasswordHash : DummyHash;
            var ok = PasswordHasher.Verify(password, hash) && usable;

            if (!ok)
            {
                throttle.Fail(username);
                await audit.LogAsync(shortName, "Başarısız giriş", "Kullanıcı adı veya parola hatalı");
                return Results.Redirect(Fail(1));
            }

            // 3) Başarılı: kimlik kartı (claims) oluştur, cookie ver
            throttle.Reset(username);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user!.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new("display_name", user.DisplayName),
                new(ClaimTypes.Role, user.Role.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                                   new ClaimsPrincipal(identity));

            user.LastLoginAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await audit.LogAsync(user.Username, "Giriş", "Başarılı giriş");

            return Results.Redirect(target);
        });

        app.MapPost("/account/logout", async (HttpContext http, AuditLogger audit) =>
        {
            var name = http.User.Identity?.Name ?? "?";
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await audit.LogAsync(name, "Çıkış", "");
            return Results.Redirect("/login");
        }).DisableAntiforgery();
    }

    // Sadece site içi yollara yönlendir (open redirect saldırısını önler).
    private static string SafeLocalUrl(string? url) =>
        !string.IsNullOrEmpty(url) && url.StartsWith('/')
        && !url.StartsWith("//") && !url.StartsWith("/\\")
            ? url : "/";
}