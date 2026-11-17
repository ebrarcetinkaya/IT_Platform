using BtPlatform.Core.Entities;

namespace BtPlatform.Core.Services;

public record RealComputerResult(Computer? Computer, int AppCount, bool Created, List<string> Errors)
{
    public bool Ok => Computer is not null;
    public static RealComputerResult Fail(params string[] errors) => new(null, 0, false, errors.ToList());
}

public interface IRealComputerService
{
    // Bu bilgisayarı toplayıp envantere ekler; zaten gerçek kayıt varsa günceller (hep tek gerçek kayıt).
    Task<RealComputerResult> CollectAndSaveAsync(RealComputerForm form, CancellationToken ct = default);

    // Mevcut gerçek kayıt (yoksa null)
    Task<Computer?> GetRealAsync();
}