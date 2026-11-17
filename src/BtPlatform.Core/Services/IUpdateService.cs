using BtPlatform.Core.Entities;

namespace BtPlatform.Core.Services;

public record UpdateRequestResult(List<UpdateJob> Created, List<string> Skipped, Guid BatchId);

// YENİ: /software sayfasında bir satır = bir bilgisayardaki bir uygulama
public record ComputerAppUpdate(Computer Computer, AppUpdateInfo Info);

public interface IUpdateService
{
    // Bir bilgisayardaki uygulamaların güncelleme durumları
    Task<List<AppUpdateInfo>> GetAppUpdatesAsync(int computerId);

    // YENİ: Departman filtresi için bilgisayarlardaki departman adları (tekrarsız, sıralı)
    Task<List<string>> GetDepartmentsAsync();

    // YENİ: Tüm bilgisayarlardaki (ya da seçilen departmandaki) uygulamaların güncelleme durumları.
    // department boş/null ise hepsi gelir.
    Task<List<ComputerAppUpdate>> GetSoftwareOverviewAsync(string? department);

    // Seçilen uygulama kayıtları için iş oluşturur (tek ya da çok bilgisayar). Uygun olmayanlar atlanır.
    Task<UpdateRequestResult> CreateJobsAsync(string requestedBy, IReadOnlyCollection<int> installedAppIds);

    Task<List<UpdateJob>> GetJobsAsync(int? computerId, int take = 50);
    // Başarısız bir işi yeniden dener (kuralları baştan değerlendirir, yeni iş açar)
    Task<UpdateRequestResult> RetryAsync(string requestedBy, int jobId);
}