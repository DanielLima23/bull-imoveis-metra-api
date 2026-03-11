using Imoveis.Domain.Entities;
using Imoveis.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Imoveis.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public const string DatabaseSchema = "imoveis";

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyRentReference> PropertyRentReferences => Set<PropertyRentReference>();
    public DbSet<PropertyHistoryEntry> PropertyHistoryEntries => Set<PropertyHistoryEntry>();
    public DbSet<PropertyDocument> PropertyDocuments => Set<PropertyDocument>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<LeaseContract> LeaseContracts => Set<LeaseContract>();
    public DbSet<Party> Parties => Set<Party>();
    public DbSet<PropertyPartyLink> PropertyPartyLinks => Set<PropertyPartyLink>();

    public DbSet<ExpenseType> ExpenseTypes => Set<ExpenseType>();
    public DbSet<PropertyExpense> PropertyExpenses => Set<PropertyExpense>();
    public DbSet<ExpenseInstallment> ExpenseInstallments => Set<ExpenseInstallment>();

    public DbSet<PendencyType> PendencyTypes => Set<PendencyType>();
    public DbSet<PendencyItem> PendencyItems => Set<PendencyItem>();

    public DbSet<PropertyVisit> PropertyVisits => Set<PropertyVisit>();
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(180).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Property>(entity =>
        {
            entity.ToTable("properties");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Title).HasMaxLength(180).IsRequired();
            entity.Property(x => x.AddressLine1).HasMaxLength(220).IsRequired();
            entity.Property(x => x.City).HasMaxLength(120).IsRequired();
            entity.Property(x => x.State).HasMaxLength(2).IsRequired();
            entity.Property(x => x.ZipCode).HasMaxLength(12).IsRequired();
            entity.Property(x => x.PropertyType).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.RegistrationNumber).HasMaxLength(120);
            entity.Property(x => x.DeedNumber).HasMaxLength(120);
            entity.Property(x => x.RegistrationCertificate).HasMaxLength(180);
            entity.Property(x => x.VacancyReason).HasMaxLength(250);
        });

        modelBuilder.Entity<PropertyRentReference>(entity =>
        {
            entity.ToTable("property_rent_references");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.EffectiveFrom).IsRequired();
            entity.HasIndex(x => new { x.PropertyId, x.EffectiveFrom });
            entity.HasOne(x => x.Property)
                .WithMany(x => x.RentReferences)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.DocumentNumber).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => x.DocumentNumber).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(30).IsRequired();
            entity.Property(x => x.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Party>(entity =>
        {
            entity.ToTable("parties");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(180).IsRequired();
            entity.Property(x => x.DocumentNumber).HasMaxLength(40);
            entity.Property(x => x.Email).HasMaxLength(180);
            entity.Property(x => x.Phone).HasMaxLength(40);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<PropertyPartyLink>(entity =>
        {
            entity.ToTable("property_party_links");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasOne(x => x.Property)
                .WithMany(x => x.PartyLinks)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Party)
                .WithMany(x => x.PropertyLinks)
                .HasForeignKey(x => x.PartyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.PropertyId, x.PartyId, x.Role });
        });

        modelBuilder.Entity<LeaseContract>(entity =>
        {
            entity.ToTable("lease_contracts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StartDate).IsRequired();
            entity.Property(x => x.EndDate);
            entity.Property(x => x.MonthlyRent).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.DepositAmount).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.AdjustmentIndex).HasMaxLength(50);
            entity.Property(x => x.PaymentLocation).HasMaxLength(180);
            entity.Property(x => x.GuaranteeType).HasMaxLength(60);
            entity.Property(x => x.GuaranteeDetails).HasMaxLength(1000);
            entity.Property(x => x.Notes).HasMaxLength(2000);

            entity.HasOne(x => x.Property)
                .WithMany(x => x.Leases)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Tenant)
                .WithMany(x => x.Leases)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExpenseType>(entity =>
        {
            entity.ToTable("expense_types");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(80).IsRequired();
            entity.Property(x => x.IsFixedCost).HasDefaultValue(false);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<PropertyExpense>(entity =>
        {
            entity.ToTable("property_expenses");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Description).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Frequency).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.DueDate).IsRequired();
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);

            entity.HasOne(x => x.Property)
                .WithMany(x => x.Expenses)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ExpenseType)
                .WithMany(x => x.Expenses)
                .HasForeignKey(x => x.ExpenseTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExpenseInstallment>(entity =>
        {
            entity.ToTable("expense_installments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.PaidAmount).HasPrecision(18, 2);
            entity.HasOne(x => x.PropertyExpense)
                .WithMany(x => x.Installments)
                .HasForeignKey(x => x.PropertyExpenseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.PropertyExpenseId, x.InstallmentNumber }).IsUnique();
        });

        modelBuilder.Entity<PendencyType>(entity =>
        {
            entity.ToTable("pendency_types");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Acronym).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.DefaultSlaDays).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.Acronym).IsUnique();
        });

        modelBuilder.Entity<PendencyItem>(entity =>
        {
            entity.ToTable("pendency_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.HasOne(x => x.Property)
                .WithMany(x => x.Pendencies)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PendencyType)
                .WithMany(x => x.Pendencies)
                .HasForeignKey(x => x.PendencyTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PropertyVisit>(entity =>
        {
            entity.ToTable("property_visits");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ContactName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.ContactPhone).HasMaxLength(30);
            entity.Property(x => x.ResponsibleName).HasMaxLength(120);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasOne(x => x.Property)
                .WithMany(x => x.Visits)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PropertyHistoryEntry>(entity =>
        {
            entity.ToTable("property_history_entries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.HasOne(x => x.Property)
                .WithMany(x => x.HistoryEntries)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.PropertyId, x.OccurredAtUtc });
        });

        modelBuilder.Entity<PropertyDocument>(entity =>
        {
            entity.ToTable("property_documents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Kind).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Url).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasOne(x => x.Property)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MaintenanceRequest>(entity =>
        {
            entity.ToTable("maintenance_requests");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Priority).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.EstimatedCost).HasPrecision(18, 2);
            entity.Property(x => x.ActualCost).HasPrecision(18, 2);
            entity.HasOne(x => x.Property)
                .WithMany(x => x.MaintenanceRequests)
                .HasForeignKey(x => x.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SystemSettings>(entity =>
        {
            entity.ToTable("system_settings");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.BrandName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.BrandShortName).HasMaxLength(8).IsRequired();
            entity.Property(x => x.ThemePreset).HasMaxLength(40).IsRequired().HasDefaultValue("AURORA_LIGHT");
            entity.Property(x => x.PrimaryColor).HasMaxLength(7).IsRequired();
            entity.Property(x => x.SecondaryColor).HasMaxLength(7).IsRequired();
            entity.Property(x => x.AccentColor).HasMaxLength(7).IsRequired();
            entity.Property(x => x.EnableAnimations).HasDefaultValue(true);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not Imoveis.Domain.Common.BaseEntity entity)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAtUtc = now;
            }

            if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAtUtc = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
