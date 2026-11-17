using BtPlatform.Core.Entities;

namespace BtPlatform.Core.Services;

public enum UpdateState
{
    UpToDate,        // güncel
    UpdateAvailable, // yeni sürüm var, güncellenebilir
    JobActive,       // zaten bir güncelleme işi sürüyor
    NotInCatalog,    // katalogda yok, güncelleme sürümünü bilmiyoruz
    Unauthorized,    // yetkisiz yazılım: güncellenmez, kaldırılmalı
    RealComputer     // gerçek makineden toplanan kayıt: güncelleme kapalı
}

public record AppUpdateInfo(InstalledApp App, SoftwareCatalogItem? Catalog, UpdateState State)
{
    public bool CanUpdate => State == UpdateState.UpdateAvailable;
}

// "Bu uygulama güncellenebilir mi?" kararının TEK yeri. Hem arayüz hem iş oluşturma bunu kullanır.
public static class UpdateRules
{
    // isRealComputer isteğe bağlı: mevcut çağrılar ve testler aynen çalışır
    public static UpdateState Evaluate(InstalledApp app, SoftwareCatalogItem? catalog, bool hasActiveJob,
        bool isRealComputer = false)
    {
        // Yetkisiz yazılımı güncellemek anlamsız (hatta zararlı): kaldırılması gerekir
        if (!app.IsAuthorized) return UpdateState.Unauthorized;

        // Gerçek makineye sahte kurulum yapılmaz: ajan simülasyonu gerçekte hiçbir şey kurmaz
        if (isRealComputer) return UpdateState.RealComputer;

        if (catalog is null) return UpdateState.NotInCatalog;
        if (hasActiveJob) return UpdateState.JobActive;

        return SoftwareVersion.IsOlder(app.Version, catalog.LatestVersion)
            ? UpdateState.UpdateAvailable
            : UpdateState.UpToDate;
    }
}