namespace BtPlatform.Core.Services;

// Tarama sonucu: Error doluysa tarama başarısız; boş liste + Error yok = güncellenecek uygulama yok.
public record WingetScanResult(IReadOnlyList<WingetUpgrade> Items, string? Error)
{
    public bool Ok => Error is null;
    public static WingetScanResult Success(IReadOnlyList<WingetUpgrade> items) => new(items, null);
    public static WingetScanResult Fail(string error) => new(new List<WingetUpgrade>(), error);
}

public interface IWingetScanner
{
    // Bu bilgisayardaki güncellenebilir uygulamaları SALT OKUNUR listeler. Hiçbir şey kurmaz.
    Task<WingetScanResult> ScanAsync(CancellationToken ct = default);
}