using AspNetCoreDomainLab.Infrastructure.Authentication;
using AspNetCoreDomainLab.Modules.Banking;
using AspNetCoreDomainLab.Modules.Healthcare;
using AspNetCoreDomainLab.Modules.IndustrialAutomation;
using AspNetCoreDomainLab.Modules.Logistics;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreDomainLab.Infrastructure.Persistence;

// Bab 12 — satu context dipakai untuk satu database, tetapi setiap entity memiliki
// schema/table mapping eksplisit. Ini menunjukkan ownership tanpa membuat empat context.
public sealed class DomainDbContext(DbContextOptions<DomainDbContext> options) : DbContext(options)
{
    public DbSet<AuthUser> Users => Set<AuthUser>();

    public DbSet<AuthUserRole> UserRoles => Set<AuthUserRole>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<LabResult> LabResults => Set<LabResult>();

    public DbSet<MedicalInventoryItem> MedicalInventoryItems => Set<MedicalInventoryItem>();

    public DbSet<IndustrialAsset> IndustrialAssets => Set<IndustrialAsset>();

    public DbSet<IndustrialTelemetryReading> IndustrialTelemetryReadings => Set<IndustrialTelemetryReading>();

    public DbSet<IndustrialAlarm> IndustrialAlarms => Set<IndustrialAlarm>();

    public DbSet<MaintenanceWorkOrder> MaintenanceWorkOrders => Set<MaintenanceWorkOrder>();

    public DbSet<DeviceCommand> DeviceCommands => Set<DeviceCommand>();

    public DbSet<Shipment> Shipments => Set<Shipment>();

    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();

    public DbSet<DeliveryRoute> DeliveryRoutes => Set<DeliveryRoute>();

    public DbSet<RouteStop> RouteStops => Set<RouteStop>();

    public DbSet<CarrierEvent> CarrierEvents => Set<CarrierEvent>();

    public DbSet<BankingCustomer> BankingCustomers => Set<BankingCustomer>();

    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();

    public DbSet<PaymentTransfer> PaymentTransfers => Set<PaymentTransfer>();

    public DbSet<AccountTransaction> AccountTransactions => Set<AccountTransaction>();

    public DbSet<FraudRiskReview> FraudRiskReviews => Set<FraudRiskReview>();

    // Bab 12: fluent mapping menetapkan key, length, index, relationship, delete behavior,
    // dan concurrency token yang tidak boleh diserahkan pada convention default.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuthUser>(entity =>
        {
            entity.ToTable("users", DatabaseSchemas.Auth);
            entity.HasKey(user => user.Id);
            entity.Property(user => user.UserName).HasMaxLength(64).IsRequired();
            entity.Property(user => user.NormalizedUserName).HasMaxLength(64).IsRequired();
            entity.Property(user => user.DisplayName).HasMaxLength(128).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(user => user.CreatedAtUtc).IsRequired();
            entity.HasIndex(user => user.NormalizedUserName).IsUnique();
            entity.HasMany(user => user.Roles)
                .WithOne(role => role.User)
                .HasForeignKey(role => role.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(user => user.RefreshTokens)
                .WithOne(token => token.User)
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuthUserRole>(entity =>
        {
            entity.ToTable("user_roles", DatabaseSchemas.Auth);
            entity.HasKey(role => new { role.UserId, role.Role });
            entity.Property(role => role.Role).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens", DatabaseSchemas.Auth);
            entity.HasKey(token => token.Id);
            entity.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
            entity.Property(token => token.Version).IsConcurrencyToken().IsRequired();
            entity.Property(token => token.RevocationReason).HasMaxLength(128);
            entity.Property(token => token.ReplacedByTokenHash).HasMaxLength(64);
            entity.Property(token => token.CreatedAtUtc).IsRequired();
            entity.Property(token => token.ExpiresAtUtc).IsRequired();
            entity.HasIndex(token => token.TokenHash).IsUnique();
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("patients", DatabaseSchemas.Healthcare);
            entity.HasKey(patient => patient.Id);
            entity.Property(patient => patient.MedicalRecordNumber).HasMaxLength(32).IsRequired();
            entity.Property(patient => patient.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(patient => patient.BirthDate).HasColumnType("date").IsRequired();
            entity.Property(patient => patient.CreatedAtUtc).IsRequired();
            entity.HasIndex(patient => patient.MedicalRecordNumber).IsUnique();
            entity.HasMany(patient => patient.Appointments)
                .WithOne(appointment => appointment.Patient)
                .HasForeignKey(appointment => appointment.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(patient => patient.LabResults)
                .WithOne(result => result.Patient)
                .HasForeignKey(result => result.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("appointments", DatabaseSchemas.Healthcare);
            entity.HasKey(appointment => appointment.Id);
            entity.Property(appointment => appointment.Provider).HasMaxLength(120).IsRequired();
            entity.Property(appointment => appointment.Status).HasMaxLength(32).IsRequired();
            entity.Property(appointment => appointment.StartsAtUtc).IsRequired();
            entity.HasIndex(appointment => new { appointment.PatientId, appointment.StartsAtUtc });
        });

        modelBuilder.Entity<LabResult>(entity =>
        {
            entity.ToTable("lab_results", DatabaseSchemas.Healthcare);
            entity.HasKey(result => result.Id);
            entity.Property(result => result.TestName).HasMaxLength(120).IsRequired();
            entity.Property(result => result.ResultValue).HasMaxLength(80).IsRequired();
            entity.Property(result => result.Unit).HasMaxLength(32).IsRequired();
            entity.Property(result => result.Status).HasMaxLength(32).IsRequired();
            entity.Property(result => result.CollectedAtUtc).IsRequired();
        });

        modelBuilder.Entity<MedicalInventoryItem>(entity =>
        {
            entity.ToTable("medical_inventory", DatabaseSchemas.Healthcare);
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Sku).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(120).IsRequired();
            entity.Property(item => item.UpdatedAtUtc).IsRequired();
            entity.HasIndex(item => item.Sku).IsUnique();
        });

        modelBuilder.Entity<IndustrialAsset>(entity =>
        {
            entity.ToTable("assets", DatabaseSchemas.Industrial);
            entity.HasKey(asset => asset.Id);
            entity.Property(asset => asset.AssetCode).HasMaxLength(40).IsRequired();
            entity.Property(asset => asset.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(asset => asset.Status).HasMaxLength(32).IsRequired();
            entity.Property(asset => asset.Version).IsConcurrencyToken().IsRequired();
            entity.HasIndex(asset => asset.AssetCode).IsUnique();
            entity.HasMany(asset => asset.Telemetry)
                .WithOne(reading => reading.Asset)
                .HasForeignKey(reading => reading.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(asset => asset.Alarms)
                .WithOne(alarm => alarm.Asset)
                .HasForeignKey(alarm => alarm.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(asset => asset.WorkOrders)
                .WithOne(order => order.Asset)
                .HasForeignKey(order => order.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IndustrialTelemetryReading>(entity =>
        {
            entity.ToTable("telemetry", DatabaseSchemas.Industrial);
            entity.HasKey(reading => reading.Id);
            entity.Property(reading => reading.Unit).HasMaxLength(32).IsRequired();
            entity.Property(reading => reading.Value).HasPrecision(18, 4).IsRequired();
            entity.HasIndex(reading => new { reading.AssetId, reading.ObservedAtUtc });
        });

        modelBuilder.Entity<IndustrialAlarm>(entity =>
        {
            entity.ToTable("alarms", DatabaseSchemas.Industrial);
            entity.HasKey(alarm => alarm.Id);
            entity.Property(alarm => alarm.Severity).HasMaxLength(32).IsRequired();
            entity.Property(alarm => alarm.Message).HasMaxLength(240).IsRequired();
            entity.Property(alarm => alarm.Status).HasMaxLength(32).IsRequired();
            entity.HasIndex(alarm => new { alarm.AssetId, alarm.Status });
        });

        modelBuilder.Entity<MaintenanceWorkOrder>(entity =>
        {
            entity.ToTable("maintenance_work_orders", DatabaseSchemas.Industrial);
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Title).HasMaxLength(160).IsRequired();
            entity.Property(order => order.Priority).HasMaxLength(32).IsRequired();
            entity.Property(order => order.Status).HasMaxLength(32).IsRequired();
        });

        modelBuilder.Entity<DeviceCommand>(entity =>
        {
            entity.ToTable("device_commands", DatabaseSchemas.Industrial);
            entity.HasKey(command => command.Id);
            entity.Property(command => command.IdempotencyKey).HasMaxLength(64).IsRequired();
            entity.Property(command => command.CommandType).HasMaxLength(64).IsRequired();
            entity.Property(command => command.Status).HasMaxLength(48).IsRequired();
            entity.HasIndex(command => command.IdempotencyKey).IsUnique();
        });

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.ToTable("shipments", DatabaseSchemas.Logistics);
            entity.HasKey(shipment => shipment.Id);
            entity.Property(shipment => shipment.TrackingNumber).HasMaxLength(40).IsRequired();
            entity.Property(shipment => shipment.Status).HasMaxLength(32).IsRequired();
            entity.Property(shipment => shipment.Origin).HasMaxLength(120).IsRequired();
            entity.Property(shipment => shipment.Destination).HasMaxLength(120).IsRequired();
            entity.Property(shipment => shipment.CurrentLocation).HasMaxLength(120).IsRequired();
            entity.HasIndex(shipment => shipment.TrackingNumber).IsUnique();
            entity.HasMany(shipment => shipment.CarrierEvents)
                .WithOne(carrierEvent => carrierEvent.Shipment)
                .HasForeignKey(carrierEvent => carrierEvent.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WarehouseStock>(entity =>
        {
            entity.ToTable("warehouse_stock", DatabaseSchemas.Logistics);
            entity.HasKey(stock => stock.Id);
            entity.Property(stock => stock.WarehouseCode).HasMaxLength(32).IsRequired();
            entity.Property(stock => stock.Sku).HasMaxLength(64).IsRequired();
            entity.HasIndex(stock => new { stock.WarehouseCode, stock.Sku }).IsUnique();
        });

        modelBuilder.Entity<DeliveryRoute>(entity =>
        {
            entity.ToTable("delivery_routes", DatabaseSchemas.Logistics);
            entity.HasKey(route => route.Id);
            entity.Property(route => route.RouteCode).HasMaxLength(40).IsRequired();
            entity.Property(route => route.Status).HasMaxLength(32).IsRequired();
            entity.HasIndex(route => route.RouteCode).IsUnique();
            entity.HasMany(route => route.Stops)
                .WithOne(stop => stop.Route)
                .HasForeignKey(stop => stop.RouteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RouteStop>(entity =>
        {
            entity.ToTable("route_stops", DatabaseSchemas.Logistics);
            entity.HasKey(stop => stop.Id);
            entity.Property(stop => stop.StopCode).HasMaxLength(40).IsRequired();
            entity.Property(stop => stop.AddressLabel).HasMaxLength(160).IsRequired();
            entity.Property(stop => stop.Status).HasMaxLength(32).IsRequired();
            entity.HasIndex(stop => new { stop.RouteId, stop.Sequence }).IsUnique();
        });

        modelBuilder.Entity<CarrierEvent>(entity =>
        {
            entity.ToTable("carrier_events", DatabaseSchemas.Logistics);
            entity.HasKey(carrierEvent => carrierEvent.Id);
            entity.Property(carrierEvent => carrierEvent.ExternalReference).HasMaxLength(80).IsRequired();
            entity.Property(carrierEvent => carrierEvent.EventType).HasMaxLength(64).IsRequired();
            entity.Property(carrierEvent => carrierEvent.PayloadSummary).HasMaxLength(240).IsRequired();
            entity.HasIndex(carrierEvent => carrierEvent.ExternalReference).IsUnique();
        });

        modelBuilder.Entity<BankingCustomer>(entity =>
        {
            entity.ToTable("customers", DatabaseSchemas.Banking);
            entity.HasKey(customer => customer.Id);
            entity.Property(customer => customer.CustomerReference).HasMaxLength(40).IsRequired();
            entity.Property(customer => customer.DisplayName).HasMaxLength(120).IsRequired();
            entity.HasIndex(customer => customer.CustomerReference).IsUnique();
            entity.HasMany(customer => customer.Accounts)
                .WithOne(account => account.Customer)
                .HasForeignKey(account => account.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BankAccount>(entity =>
        {
            entity.ToTable("accounts", DatabaseSchemas.Banking);
            entity.HasKey(account => account.Id);
            entity.Property(account => account.AccountReference).HasMaxLength(40).IsRequired();
            entity.Property(account => account.Currency).HasMaxLength(3).IsRequired();
            entity.Property(account => account.Status).HasMaxLength(32).IsRequired();
            entity.Property(account => account.Version).IsConcurrencyToken().IsRequired();
            entity.HasIndex(account => account.AccountReference).IsUnique();
            entity.HasMany(account => account.Transactions)
                .WithOne(transaction => transaction.Account)
                .HasForeignKey(transaction => transaction.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentTransfer>(entity =>
        {
            entity.ToTable("transfers", DatabaseSchemas.Banking);
            entity.HasKey(transfer => transfer.Id);
            entity.Property(transfer => transfer.Currency).HasMaxLength(3).IsRequired();
            entity.Property(transfer => transfer.Status).HasMaxLength(48).IsRequired();
            entity.Property(transfer => transfer.IdempotencyKey).HasMaxLength(80).IsRequired();
            entity.HasIndex(transfer => transfer.IdempotencyKey).IsUnique();
            entity.HasOne(transfer => transfer.FromAccount)
                .WithMany()
                .HasForeignKey(transfer => transfer.FromAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(transfer => transfer.ToAccount)
                .WithMany()
                .HasForeignKey(transfer => transfer.ToAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(transfer => transfer.RiskReviews)
                .WithOne(review => review.Transfer)
                .HasForeignKey(review => review.TransferId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AccountTransaction>(entity =>
        {
            entity.ToTable("account_transactions", DatabaseSchemas.Banking);
            entity.HasKey(transaction => transaction.Id);
            entity.Property(transaction => transaction.Direction).HasMaxLength(16).IsRequired();
            entity.Property(transaction => transaction.Description).HasMaxLength(240).IsRequired();
            entity.HasOne(transaction => transaction.Transfer)
                .WithMany()
                .HasForeignKey(transaction => transaction.TransferId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<FraudRiskReview>(entity =>
        {
            entity.ToTable("risk_reviews", DatabaseSchemas.Banking);
            entity.HasKey(review => review.Id);
            entity.Property(review => review.Status).HasMaxLength(24).IsRequired();
            entity.Property(review => review.Reason).HasMaxLength(240).IsRequired();
            entity.Property(review => review.ReviewedBy).HasMaxLength(120);
            entity.HasIndex(review => new { review.TransferId, review.Status });
        });

        // Future business entities must also opt into an owning schema explicitly. Keeping the
        // context default-schema neutral prevents a module from silently owning public tables.
    }
}
