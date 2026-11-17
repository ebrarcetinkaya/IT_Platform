namespace BtPlatform.Core.Services;

public record RealCommandResult(bool Ok, string Output);

public interface IRealCommandExecutor
{
    // Komutu bu bilgisayarda çalıştırır. Çağıran, doğrulamayı daha önce yapmış olmalıdır;
    // yine de çalıştırıcı kendi yapısal (AST) kontrolünü ayrıca yapar.
    Task<RealCommandResult> ExecuteAsync(string command, CancellationToken ct = default);
}