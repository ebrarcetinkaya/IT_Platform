using BtPlatform.Core.Entities;
using Microsoft.EntityFrameworkCore;


namespace BtPlatform.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Computer> Computers => Set<Computer>();
    public DbSet<InstalledApp> InstalledApps => Set<InstalledApp>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<NetworkSwitch> Switches => Set<NetworkSwitch>();
    public DbSet<SwitchPort> SwitchPorts => Set<SwitchPort>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<DirectoryUser> DirectoryUsers => Set<DirectoryUser>();
    public DbSet<DirectoryGroup> DirectoryGroups => Set<DirectoryGroup>();
    public DbSet<SoftwareCatalogItem> SoftwareCatalog => Set<SoftwareCatalogItem>();
    public DbSet<UpdateJob> UpdateJobs => Set<UpdateJob>();
    public DbSet<RealUpgradeFinding> RealUpgradeFindings => Set<RealUpgradeFinding>();
    public DbSet<RemoteCommandJob> RemoteCommandJobs => Set<RemoteCommandJob>();
    public DbSet<PcEvent> PcEvents => Set<PcEvent>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Computer>(e =>
        {
            e.HasIndex(c => c.Hostname).IsUnique();
            e.HasIndex(c => c.AssetTag).IsUnique();
            e.Property(c => c.Status).HasConversion<string>();
        });

        modelBuilder.Entity<InstalledApp>(e =>
        {
            e.HasOne(a => a.Computer)
             .WithMany(c => c.Apps)
             .HasForeignKey(a => a.ComputerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Ticket>(e =>
        {
            e.Property(t => t.Status).HasConversion<string>();
            e.Property(t => t.Priority).HasConversion<string>();
            e.Property(t => t.Category).HasConversion<string>();
            e.HasOne(t => t.Computer)
             .WithMany()
             .HasForeignKey(t => t.ComputerId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<NetworkSwitch>(e =>
        {
            e.HasIndex(s => s.Name).IsUnique();
        });

        modelBuilder.Entity<SwitchPort>(e =>
        {
            e.Property(p => p.Status).HasConversion<string>();
            e.HasOne(p => p.Switch)
             .WithMany(s => s.Ports)
             .HasForeignKey(p => p.SwitchId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Alert>(e =>
        {
            e.Property(a => a.Severity).HasConversion<string>();
            e.HasOne(a => a.Switch)
             .WithMany()
             .HasForeignKey(a => a.SwitchId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AppUser>(e =>
    {
        e.HasIndex(u => u.Username).IsUnique();
        e.Property(u => u.Role).HasConversion<string>();
    });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasIndex(a => a.CreatedAt);
        });

        modelBuilder.Entity<DirectoryUser>(e =>
{
    e.HasIndex(u => u.Username).IsUnique();
    // Çoka-çok ilişki: bir kullanıcı birden çok grupta, bir grupta birden çok kullanıcı olabilir.
    // EF Core ara tabloyu (DirectoryGroupDirectoryUser) kendisi oluşturur.
    e.HasMany(u => u.Groups).WithMany(g => g.Members);
});

        modelBuilder.Entity<DirectoryGroup>(e =>
        {
            e.HasIndex(g => g.Name).IsUnique();
        });

        modelBuilder.Entity<SoftwareCatalogItem>(e =>
        {
            e.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<UpdateJob>(e =>
{
    e.Property(j => j.Status).HasConversion<string>();
    e.HasOne(j => j.Computer).WithMany()
        .HasForeignKey(j => j.ComputerId).OnDelete(DeleteBehavior.SetNull);
    e.HasOne(j => j.InstalledApp).WithMany()
        .HasForeignKey(j => j.InstalledAppId).OnDelete(DeleteBehavior.SetNull);
    e.HasIndex(j => j.Status);      // ajan "bekleyen iş var mı?" diye sık sorar
    e.HasIndex(j => j.BatchId);

    modelBuilder.Entity<RealUpgradeFinding>(e =>
    {
        e.HasOne(f => f.Computer).WithMany()
            .HasForeignKey(f => f.ComputerId).OnDelete(DeleteBehavior.Cascade);
        e.HasIndex(f => f.ComputerId);
    });


    modelBuilder.Entity<RemoteCommandJob>(e =>
    {
        e.Property(j => j.Status).HasConversion<string>();
        // Bilgisayar silinse bile komut geçmişi (denetim izi) kalsın
        e.HasOne(j => j.Computer).WithMany()
            .HasForeignKey(j => j.ComputerId).OnDelete(DeleteBehavior.SetNull);
        e.HasIndex(j => j.Status);      // ajan "bekleyen komut var mı?" diye sık sorar
        e.HasIndex(j => j.ComputerId);
    });


    modelBuilder.Entity<PcEvent>(e =>
    {
        e.Property(p => p.Level).HasConversion<string>();
        // Bilgisayar silinirse olayları da gider (yetim kayıt kalmasın)
        e.HasOne(p => p.Computer).WithMany()
            .HasForeignKey(p => p.ComputerId).OnDelete(DeleteBehavior.Cascade);
        // Aynı olay (aynı günlük ve kayıt numarası) iki kez yazılamaz
        e.HasIndex(p => new { p.ComputerId, p.LogName, p.RecordId }).IsUnique();
        e.HasIndex(p => p.OccurredAt);
    });
});

    }
}