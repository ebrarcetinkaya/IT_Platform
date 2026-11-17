using BtPlatform.Core.Services;

namespace BtPlatform.Web.Services;

// Sunucu açılınca gerçek kayıt yoksa bu bilgisayarı kendiliğinden envantere ekler.
// Kayıt zaten varsa hiçbir şey yapmaz (formla düzeltilmiş bilgilerin üzerine yazmaz).
public class RealComputerBootstrapService : BackgroundService
{
    private const int MaxAttempts = 3;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RealComputerBootstrapService> _logger;

    public RealComputerBootstrapService(IServiceScopeFactory scopeFactory,
        ILogger<RealComputerBootstrapService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); }   // açılış yavaşlamasın
        catch (OperationCanceledException) { return; }

        for (var attempt = 1; attempt <= MaxAttempts && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IRealComputerService>();

                if (await service.GetRealAsync() is not null) return;   // zaten var: dokunma

                // Makineden okunamayan alanlar için varsayılanlar (formla sonradan düzeltilebilir)
                var form = new RealComputerForm(
                    AssetTag: "REAL-001",
                    AssignedUser: $"{Environment.UserName} (BT Stajyeri)",
                    Department: "Bilgi İşlem",
                    Location: "Bilgi İşlem (Stajyer)",
                    Vlan: 10,
                    WarrantyEnd: DateTime.UtcNow.AddYears(1));   // gerçek garanti bilinmiyor

                var result = await service.CollectAndSaveAsync(form, stoppingToken);
                if (result.Ok)
                {
                    var audit = scope.ServiceProvider.GetRequiredService<AuditLogger>();
                    await audit.LogAsync("sistem", "Gerçek bilgisayar otomatik eklendi",
                        $"{result.Computer!.Hostname} · etiket {result.Computer.AssetTag} · {result.AppCount} yazılım");
                    _logger.LogInformation("Gerçek bilgisayar otomatik eklendi: {Host}", result.Computer.Hostname);
                    return;
                }

                _logger.LogWarning("Otomatik ekleme başarısız (deneme {N}/{Max}): {Errors}",
                    attempt, MaxAttempts, string.Join("; ", result.Errors));
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Otomatik ekleme hatası (deneme {N}/{Max})", attempt, MaxAttempts);
            }

            try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }
}