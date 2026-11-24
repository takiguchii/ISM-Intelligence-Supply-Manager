using System.Linq.Expressions;
using System.Reflection;
using ISM.Application.Security;
using ISM.Domain.Entities;
using ISM.Domain.Modules.Stock.Entities;
using ISM.Domain.Modules.Suppliers.Entities;
using ISM.Domain.Modules.System.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ISM.Domain.Modules.DataImport;
using ISM.Domain.Modules.Menu.Entities;
using ISM.Domain.Modules.Orders;
using ISM.Domain.Modules.Finance;
using ISM.Domain.Modules.Assistant.Entities;

namespace ISM.Infrastructure.Data.Context;

public sealed class IsmDbContext : DbContext
{
    private readonly ICurrentUser? _currentUser;

    public IsmDbContext(DbContextOptions<IsmDbContext> options, ICurrentUser? currentUser = null)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<FinanceEntry> FinanceEntries => Set<FinanceEntry>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<SupplierProductPriceHistory> SupplierProductPriceHistories => Set<SupplierProductPriceHistory>();
    public DbSet<SystemAlert> SystemAlerts => Set<SystemAlert>();
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Dish> Dishes => Set<Dish>();
    public DbSet<DishIngredient> DishIngredients => Set<DishIngredient>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ImportAudit> ImportAudits => Set<ImportAudit>();
    public DbSet<ReceiptConfirmation> ReceiptConfirmations => Set<ReceiptConfirmation>();
    public DbSet<ImportErrorLog> ImportErrorLogs => Set<ImportErrorLog>();
    public DbSet<TmpAuthorization> TmpAuthorizations => Set<TmpAuthorization>();
    public DbSet<AssistantProposal> AssistantProposals => Set<AssistantProposal>();
    public DbSet<AssistantActionAudit> AssistantActionAudits => Set<AssistantActionAudit>();
    public DbSet<AssistantThread> AssistantThreads => Set<AssistantThread>();
    public DbSet<AssistantMessage> AssistantMessages => Set<AssistantMessage>();
    public DbSet<AutomationPolicy> AutomationPolicies => Set<AutomationPolicy>();
    public DbSet<AutomationRun> AutomationRuns => Set<AutomationRun>();

    internal int? CurrentUserRestaurantId =>
        _currentUser != null && !_currentUser.IsSuperAdmin
            ? _currentUser.RestaurantId
            : null;

    internal bool ApplyTenantFilter => _currentUser != null && !_currentUser.IsSuperAdmin;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReceiptConfirmation>(builder =>
        {
            builder.ToTable("receipt_confirmations");
            builder.HasKey(x => x.ImportId);
            builder.Property(x => x.ImportId).ValueGeneratedNever();
            builder.Property(x => x.ContentSha256).HasMaxLength(64).IsRequired();
            builder.Property(x => x.DocumentIdentitySha256).HasMaxLength(64).IsRequired();
            builder.Property(x => x.IdempotencyKey).HasMaxLength(80).IsRequired();
            builder.Property(x => x.RequestSha256).HasMaxLength(64).IsRequired();
            builder.Property(x => x.ConfirmedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.HasIndex(x => new { x.RestaurantId, x.ContentSha256 }).IsUnique();
            builder.HasIndex(x => new { x.RestaurantId, x.DocumentIdentitySha256 }).IsUnique();
            builder.HasIndex(x => new { x.RestaurantId, x.IdempotencyKey }).IsUnique();
            builder.HasOne<Restaurant>().WithMany().HasForeignKey(x => x.RestaurantId).OnDelete(DeleteBehavior.Cascade);
            builder.ApplyTenantQueryFilter(this);
        });
        modelBuilder.Entity<NotificationDelivery>(builder =>
        {
            builder.ToTable("notification_deliveries");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.Channel).HasMaxLength(16);
            builder.Property(d => d.Status).HasMaxLength(16).IsConcurrencyToken();
            builder.Property(d => d.Attempts).IsConcurrencyToken();
            builder.Property(d => d.Recipient).HasMaxLength(150);
            builder.Property(d => d.Subject).HasMaxLength(200);
            builder.Property(d => d.Body).HasMaxLength(6000);
            builder.Property(d => d.HtmlBody).HasMaxLength(30000);
            builder.Property(d => d.Scenario).HasMaxLength(40);
            builder.Property(d => d.LastError).HasMaxLength(300);
            builder.HasOne(d => d.Alert).WithMany().HasForeignKey(d => d.AlertId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(d => new { d.AlertId, d.UserId, d.Channel }).IsUnique();
            builder.HasIndex(d => new { d.Status, d.NextAttemptAtUtc });
            builder.HasOne<User>().WithMany().HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<DemoInvoice>(builder =>
        {
            builder.ToTable("demo_invoices");
            builder.HasKey(i => i.Id);
            builder.Property(i => i.PlanName).HasMaxLength(50);
            builder.Property(i => i.Amount).HasPrecision(10, 2);
            builder.HasIndex(i => new { i.RestaurantId, i.CreatedAtUtc });
            builder.HasOne<Restaurant>().WithMany().HasForeignKey(i => i.RestaurantId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<AuthSession>(builder =>
        {
            builder.ToTable("auth_sessions");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).HasMaxLength(36);
            builder.Property(s => s.Device).HasMaxLength(300);
            builder.Property(s => s.IpAddress).HasMaxLength(64);
            builder.HasIndex(s => new { s.UserId, s.ExpiresAtUtc });
            builder.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<FinanceEntry>(builder =>
        {
            builder.ToTable("finance_entries");
            builder.HasKey(entry => entry.Id);
            builder.Property(entry => entry.Description).HasMaxLength(150).IsRequired();
            builder.Property(entry => entry.Category).HasMaxLength(80).IsRequired().HasDefaultValue("Outros");
            builder.Property(entry => entry.Type).HasConversion<int>();
            builder.Property(entry => entry.Amount).HasColumnType("decimal(10,2)");
            builder.HasIndex(entry => new { entry.RestaurantId, entry.DueDate });
            builder.HasOne<Restaurant>().WithMany().HasForeignKey(entry => entry.RestaurantId).OnDelete(DeleteBehavior.Restrict);
            builder.ApplyTenantQueryFilter(this);
        });

        modelBuilder.Entity<Order>(builder =>
        {
            builder.ToTable("orders");
            builder.HasKey(order => order.Id);
            builder.Property(order => order.TableReference).HasMaxLength(100).IsRequired();
            builder.Property(order => order.Status).HasConversion<int>();
            builder.Property(order => order.RequestId).HasMaxLength(36).IsRequired();
            builder.Property(order => order.Version).IsConcurrencyToken();
            builder.HasIndex(order => new { order.RestaurantId, order.RequestId }).IsUnique();
            builder.HasIndex(order => new { order.RestaurantId, order.CreatedAtUtc });
            builder.HasOne<Restaurant>().WithMany().HasForeignKey(order => order.RestaurantId).OnDelete(DeleteBehavior.Restrict);
            builder.ApplyTenantQueryFilter(this);
        });

        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.ToTable("order_items");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.DishName).HasMaxLength(150).IsRequired();
            builder.Property(item => item.Observation).HasMaxLength(500).IsRequired();
            builder.Property(item => item.UnitPrice).HasColumnType("decimal(10,2)");
            builder.Property(item => item.UnitCost).HasColumnType("decimal(10,2)");
            builder.HasOne(item => item.Order).WithMany(order => order.Items).HasForeignKey(item => item.OrderId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<Dish>().WithMany().HasForeignKey(item => item.DishId).OnDelete(DeleteBehavior.Restrict);
            builder.ApplyTenantQueryFilter(this);
        });

        modelBuilder.Entity<Plan>(builder =>
        {
            builder.ToTable("plans");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Name).HasMaxLength(50).IsRequired();
            builder.Property(p => p.Description).HasMaxLength(200);
            builder.Property(p => p.MonthlyPrice).HasColumnType("decimal(10,2)").IsRequired();
            builder.Property(p => p.MaxUsers).IsRequired();
            builder.Property(p => p.MaxDishes).IsRequired();
            builder.Property(p => p.MaxProducts).IsRequired();
            builder.Property(p => p.MaxCategories).IsRequired();
            builder.Property(p => p.IsActive).IsRequired();
            builder.Property(p => p.CreatedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(p => p.UpdatedAtUtc).HasColumnType("datetime(6)");
            builder.HasIndex(p => p.Name).IsUnique();
        });

        modelBuilder.Entity<Supplier>(builder =>
        {
            builder.ToTable("suppliers");
            builder.HasKey(f => f.Id);
            builder.Property(f => f.RestaurantId).IsRequired();
            builder.Property(f => f.Name).HasMaxLength(150).IsRequired();
            builder.Property(f => f.Category).HasMaxLength(100).IsRequired();
            builder.Property(f => f.Description).HasMaxLength(500).IsRequired(false);
            builder.Property(f => f.Email).HasMaxLength(150).IsRequired();
            builder.Property(f => f.Phone).HasMaxLength(20).IsRequired();
            builder.Property(f => f.IsActive).IsRequired();
            builder.Property(f => f.CreatedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(f => f.UpdatedAtUtc).HasColumnType("datetime(6)");
            builder.HasIndex(f => new { f.RestaurantId, f.Name }).IsUnique();

            builder.HasOne(f => f.Restaurant)
                .WithMany()
                .HasForeignKey(f => f.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ApplyTenantQueryFilter(this);
        });

        modelBuilder.Entity<Product>(builder =>
        {
            builder.ToTable("products");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.RestaurantId).IsRequired();
            builder.Property(p => p.Name).HasMaxLength(120).IsRequired();
            builder.Property(p => p.Unit).HasMaxLength(10).IsRequired();
            builder.Property(p => p.CurrentQuantity).HasColumnType("decimal(10,3)").IsRequired();
            builder.Property(p => p.MinimumQuantity).HasColumnType("decimal(10,3)").IsRequired();
            builder.Property(p => p.MaximumQuantity).HasColumnType("decimal(10,3)").IsRequired();
            builder.Property(p => p.ReorderPoint).HasColumnType("decimal(10,3)").IsRequired();
            builder.Property(p => p.AverageCost).HasColumnType("decimal(10,2)").IsRequired();
            builder.Property(p => p.MovingAverageConsumption).HasColumnType("decimal(10,3)").IsRequired();
            builder.Property(p => p.LastConsumptionRecalculatedAtUtc).HasColumnType("datetime(6)");
            builder.Property(p => p.IsActive).IsRequired();
            builder.Property(p => p.CreatedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(p => p.UpdatedAtUtc).HasColumnType("datetime(6)");
            builder.HasIndex(p => new { p.RestaurantId, p.Name }).IsUnique();

            builder.HasOne(p => p.Restaurant)
                .WithMany(r => r.Products)
                .HasForeignKey(p => p.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ApplyTenantQueryFilter(this);
        });

        modelBuilder.Entity<StockMovement>(builder =>
        {
            builder.ToTable("stock_movements");
            builder.HasKey(sm => sm.Id);
            builder.Property(sm => sm.RestaurantId).IsRequired();
            builder.Property(sm => sm.ProductId).IsRequired();
            builder.Property(sm => sm.MovementType).IsRequired()
                .HasConversion<int>();
            builder.Property(sm => sm.QuantityDelta).HasColumnType("decimal(10,3)").IsRequired();
            builder.Property(sm => sm.UnitCostSnapshot).HasColumnType("decimal(10,2)");
            builder.Property(sm => sm.CreatedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(sm => sm.UpdatedAtUtc).HasColumnType("datetime(6)");
            builder.HasIndex(sm => new { sm.RestaurantId, sm.ProductId, sm.CreatedAtUtc });
            builder.HasIndex(sm => new { sm.RestaurantId, sm.CreatedAtUtc });

            builder.HasOne(sm => sm.Product)
                .WithMany()
                .HasForeignKey(sm => sm.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(sm => sm.Restaurant)
                .WithMany()
                .HasForeignKey(sm => sm.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ApplyTenantQueryFilter(this);
        });

        modelBuilder.Entity<SupplierProductPriceHistory>(builder =>
        {
            builder.ToTable("supplier_product_price_history");
            builder.HasKey(h => h.Id);
            builder.Property(h => h.RestaurantId).IsRequired();
            builder.Property(h => h.SupplierRawName).HasMaxLength(200).IsRequired();
            builder.Property(h => h.ProductRawName).HasMaxLength(200).IsRequired();
            builder.Property(h => h.Unit).HasMaxLength(10).IsRequired();
            builder.Property(h => h.UnitPrice).HasColumnType("decimal(10,2)").IsRequired();
            builder.Property(h => h.QuantityPurchased).HasColumnType("decimal(10,3)").IsRequired();
            builder.Property(h => h.NFeAccessKeyOrImportId).HasMaxLength(200);
            builder.Property(h => h.PurchasedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(h => h.CreatedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(h => h.UpdatedAtUtc).HasColumnType("datetime(6)");
            builder.HasIndex(h => new { h.RestaurantId, h.PurchasedAtUtc });
            builder.HasIndex(h => new { h.RestaurantId, h.ProductRawName, h.PurchasedAtUtc });

            builder.HasOne(h => h.Supplier)
                .WithMany()
                .HasForeignKey(h => h.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(h => h.Restaurant)
                .WithMany()
                .HasForeignKey(h => h.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ApplyTenantQueryFilter(this);
        });

        modelBuilder.Entity<SystemAlert>(builder =>
        {
            builder.ToTable("system_alerts");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.RestaurantId).IsRequired();
            builder.Property(a => a.AlertType).IsRequired().HasConversion<int>();
            builder.Property(a => a.Severity).IsRequired().HasConversion<int>();
            builder.Property(a => a.Title).HasMaxLength(150).IsRequired();
            builder.Property(a => a.Message).HasMaxLength(400).IsRequired();
            builder.Property(a => a.ReferenceEntityType).HasMaxLength(50);
            builder.Property(a => a.PayloadSerializedJson).HasMaxLength(4000);
            builder.Property(a => a.IsRead).IsRequired();
            builder.Property(a => a.IsDismissed).IsRequired();
            builder.Property(a => a.GeneratedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(a => a.ReadAtUtc).HasColumnType("datetime(6)");
            builder.Property(a => a.DismissedAtUtc).HasColumnType("datetime(6)");
            builder.Property(a => a.CreatedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(a => a.UpdatedAtUtc).HasColumnType("datetime(6)");
            builder.HasIndex(a => new { a.RestaurantId, a.AlertType, a.GeneratedAtUtc, a.IsDismissed });
            builder.HasIndex(a => new { a.RestaurantId, a.GeneratedAtUtc });

            builder.HasOne(a => a.Restaurant)
                .WithMany()
                .HasForeignKey(a => a.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.DismissedByUser)
                .WithMany()
                .HasForeignKey(a => a.DismissedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.ApplyTenantQueryFilter(this);
        });

        modelBuilder.Entity<Restaurant>(builder =>
        {
            builder.ToTable("restaurants");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Name).HasMaxLength(120).IsRequired();
            builder.Property(r => r.CNPJ).HasMaxLength(14).IsRequired();
            builder.HasIndex(r => r.CNPJ).IsUnique();
            builder.Property(r => r.Created).HasColumnType("datetime(6)").IsRequired();
            builder.Property(r => r.ServiceFeePercent).HasPrecision(5, 2);
            builder.Property(r => r.DeliveryFee).HasPrecision(10, 2);
            builder.Property(r => r.PackagingFee).HasPrecision(10, 2);
            builder.Property(r => r.PlanId);
            builder.Property(r => r.TrialEndAtUtc).HasColumnType("datetime(6)");
            builder.Property(r => r.IsPlanActive).IsRequired();

            builder.HasOne(r => r.Plan)
                .WithMany(p => p.Restaurants)
                .HasForeignKey(r => r.PlanId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Category>(builder =>
        {
            builder.ToTable("categories");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.RestaurantId).IsRequired();
            builder.Property(c => c.Name).HasMaxLength(80).IsRequired();
            builder.Property(c => c.IsActive).IsRequired();
            builder.Property(c => c.DisplayOrder).IsRequired();
            builder.Property(c => c.CreatedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(c => c.UpdatedAtUtc).HasColumnType("datetime(6)");
            builder.HasIndex(c => new { c.RestaurantId, c.Name }).IsUnique();

            builder.HasOne(c => c.Restaurant)
                .WithMany(r => r.Categories)
                .HasForeignKey(c => c.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ApplyTenantQueryFilter(this);
        });

        modelBuilder.Entity<Dish>(builder =>
        {
            builder.ToTable("dishes");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.RestaurantId).IsRequired();
            builder.Property(d => d.CategoryId).IsRequired();
            builder.Property(d => d.Name).HasMaxLength(80).IsRequired();
            builder.Property(d => d.Description).HasMaxLength(255).IsRequired(false);
            builder.Property(d => d.UrlImage).HasMaxLength(255).IsRequired(false);
            builder.Property(d => d.Cost).HasColumnType("decimal(10,2)").IsRequired();
            builder.Property(d => d.Price).HasColumnType("decimal(10,2)").IsRequired();
            builder.Property(d => d.PriceRevision).IsRequired().IsConcurrencyToken();
            builder.Property(d => d.IsActive).IsRequired();
            builder.Property(d => d.Highlight).IsRequired();
            builder.Property(d => d.DisplayOrder).IsRequired();
            builder.Property(d => d.CreatedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(d => d.UpdatedAtUtc).HasColumnType("datetime(6)");
            builder.HasIndex(d => new { d.RestaurantId, d.Name }).IsUnique();
            builder.HasIndex(d => new { d.RestaurantId, d.CategoryId });

            builder.HasOne(d => d.Category)
                .WithMany(c => c.Dishes)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.Restaurant)
                .WithMany(r => r.Dishes)
                .HasForeignKey(d => d.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.ApplyTenantQueryFilter(this);
        });

        modelBuilder.Entity<AssistantProposal>(builder =>
        {
            builder.ToTable("assistant_proposals");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Specialist).HasMaxLength(40).IsRequired();
            builder.Property(p => p.ActionType).HasMaxLength(50).IsRequired();
            builder.Property(p => p.EntityType).HasMaxLength(50).IsRequired();
            builder.Property(p => p.EntityName).HasMaxLength(150).IsRequired();
            builder.Property(p => p.Status).HasConversion<int>().IsRequired();
            builder.Property(p => p.OriginalValue).HasColumnType("decimal(10,2)");
            builder.Property(p => p.ProposedValue).HasColumnType("decimal(10,2)");
            builder.Property(p => p.SnapshotCost).HasColumnType("decimal(10,2)");
            builder.Property(p => p.SnapshotTargetMarginPercent).HasColumnType("decimal(5,2)");
            builder.Property(p => p.EvidenceJson).HasMaxLength(8000).IsRequired();
            builder.Property(p => p.IdempotencyKey).HasMaxLength(120).IsRequired();
            builder.Property(p => p.Version).IsConcurrencyToken();
            builder.HasIndex(p => new { p.RestaurantId, p.AlertId }).IsUnique();
            builder.HasIndex(p => new { p.RestaurantId, p.IdempotencyKey }).IsUnique();
            builder.HasIndex(p => new { p.RestaurantId, p.Status, p.GeneratedAtUtc });
            builder.HasOne<SystemAlert>().WithMany().HasForeignKey(p => p.AlertId).OnDelete(DeleteBehavior.Cascade);
            builder.ApplyTenantQueryFilter(this);
        });

        modelBuilder.Entity<AssistantActionAudit>(builder =>
        {
            builder.ToTable("assistant_action_audits");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Action).HasMaxLength(30).IsRequired();
            builder.Property(a => a.EntityType).HasMaxLength(50).IsRequired();
            builder.Property(a => a.ValueBefore).HasColumnType("decimal(10,2)");
            builder.Property(a => a.ValueAfter).HasColumnType("decimal(10,2)");
            builder.Property(a => a.DetailsJson).HasMaxLength(8000);
            builder.HasIndex(a => new { a.RestaurantId, a.ProposalId, a.OccurredAtUtc });
            builder.HasOne<AssistantProposal>().WithMany().HasForeignKey(a => a.ProposalId).OnDelete(DeleteBehavior.Cascade);
            builder.ApplyTenantQueryFilter(this);
        });

        modelBuilder.Entity<AutomationPolicy>(builder =>
        {
            builder.ToTable("assistant_automation_policies");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Name).HasMaxLength(120).IsRequired();
            builder.Property(p => p.Specialist).HasMaxLength(40).IsRequired();
            builder.Property(p => p.ActionType).HasMaxLength(80).IsRequired();
            builder.Property(p => p.Target).HasMaxLength(120).IsRequired();
            builder.Property(p => p.TimeZone).HasMaxLength(80).IsRequired();
            builder.Property(p => p.Status).HasConversion<int>().IsRequired();
            builder.Property(p => p.Mode).HasConversion<int>().IsRequired();
            builder.Property(p => p.Version).IsConcurrencyToken();
            builder.HasIndex(p => new { p.RestaurantId, p.Status, p.NextRunAtUtc });
            builder.HasIndex(p => new { p.RestaurantId, p.Name }).IsUnique();
            builder.HasOne<User>().WithMany().HasForeignKey(p => p.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
            builder.ApplyTenantQueryFilter(this);
        });

        modelBuilder.Entity<AutomationRun>(builder =>
        {
            builder.ToTable("assistant_automation_runs");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.IdempotencyKey).HasMaxLength(160).IsRequired();
            builder.Property(r => r.Reason).HasMaxLength(500).IsRequired();
            builder.Property(r => r.EvidenceJson).HasMaxLength(8000).IsRequired();
            builder.Property(r => r.CorrelationId).HasMaxLength(80);
            builder.Property(r => r.Status).HasConversion<int>().IsRequired();
            builder.HasIndex(r => new { r.PolicyId, r.IdempotencyKey }).IsUnique();
            builder.HasIndex(r => new { r.RestaurantId, r.StartedAtUtc });
            builder.HasOne<AutomationPolicy>().WithMany().HasForeignKey(r => r.PolicyId).OnDelete(DeleteBehavior.Cascade);
            builder.ApplyTenantQueryFilter(this);
        });

        modelBuilder.Entity<AssistantThread>(builder =>
        {
            builder.ToTable("assistant_threads");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.OriginType).HasMaxLength(40).IsRequired();
            builder.Property(t => t.SpecialistId).HasMaxLength(30).IsRequired();
            builder.Property(t => t.SourceView).HasMaxLength(80);
            builder.Property(t => t.SubjectEntityType).HasMaxLength(50);
            builder.Property(t => t.Subject).HasMaxLength(150).IsRequired();
            builder.Property(t => t.ContextSnapshotJson).HasMaxLength(12000).IsRequired();
            builder.Property(t => t.LastMessageAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(t => t.CreatedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.HasIndex(t => new { t.RestaurantId, t.UserId, t.LastMessageAtUtc });
            builder.HasIndex(t => new { t.RestaurantId, t.OriginAlertId });
            builder.HasIndex(t => new { t.RestaurantId, t.UserId, t.SpecialistId, t.LastMessageAtUtc });
            builder.HasOne<Restaurant>().WithMany().HasForeignKey(t => t.RestaurantId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<SystemAlert>().WithMany().HasForeignKey(t => t.OriginAlertId).OnDelete(DeleteBehavior.SetNull);
            builder.ApplyTenantQueryFilter(this);
        });

        modelBuilder.Entity<AssistantMessage>(builder =>
        {
            builder.ToTable("assistant_messages");
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Role).HasMaxLength(20).IsRequired();
            builder.Property(m => m.Content).HasMaxLength(6000).IsRequired();
            builder.Property(m => m.Mode).HasMaxLength(30);
            builder.Property(m => m.Source).HasMaxLength(80);
            builder.Property(m => m.CreatedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.HasIndex(m => new { m.RestaurantId, m.ThreadId, m.CreatedAtUtc });
            builder.HasOne<AssistantThread>().WithMany().HasForeignKey(m => m.ThreadId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<User>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.SetNull);
            builder.ApplyTenantQueryFilter(this);
        });

        modelBuilder.Entity<DishIngredient>(builder =>
        {
            builder.ToTable("dish_ingredients");
            builder.HasKey(di => new { di.DishId, di.ProductId });
            builder.Property(di => di.Quantity).HasColumnType("decimal(10,3)");

            builder.HasOne(di => di.Dish)
                .WithMany(d => d.Ingredients)
                .HasForeignKey(di => di.DishId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(di => di.Product)
                .WithMany()
                .HasForeignKey(di => di.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Name).HasMaxLength(150).IsRequired();
            builder.Property(u => u.Email).HasMaxLength(150).IsRequired();
            builder.HasIndex(u => u.Email).IsUnique();
            builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
            builder.Property(u => u.Role).HasMaxLength(50).IsRequired();
            builder.Property(u => u.LastTotpStep).IsConcurrencyToken();
            builder.Property(u => u.RecoveryCodeHashes).IsConcurrencyToken();
            builder.Property(u => u.NotificationEmailAddress).HasMaxLength(150);
            builder.Property(u => u.WhatsAppPhone).HasMaxLength(16);
            builder.Property(u => u.TwoFactorSecret).HasMaxLength(2000);
            builder.Property(u => u.IsActive).IsRequired();
            builder.Property(u => u.CreatedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(u => u.UpdatedAtUtc).HasColumnType("datetime(6)");

            builder.HasOne(u => u.Restaurant)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RestaurantId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.ApplyTenantQueryFilter(this);
        });

        modelBuilder.Entity<ImportAudit>(builder =>
        {
            builder.ToTable("import_audits");
            builder.HasKey(a => a.ImportId);
            builder.Property(a => a.ImportId).ValueGeneratedNever();

            builder.Property(a => a.RestaurantId).IsRequired();
            builder.Property(a => a.DataSourceName).HasMaxLength(200).IsRequired();
            builder.Property(a => a.DataSourceType).IsRequired()
                .HasConversion<string>();
            builder.Property(a => a.TargetEntity).IsRequired()
                .HasConversion<string>();
            builder.Property(a => a.UpsertStrategy).IsRequired()
                .HasConversion<string>();

            builder.Property(a => a.SourceOriginalFilename).HasMaxLength(500);
            builder.Property(a => a.SourceEndpointOrUrl).HasMaxLength(500);
            builder.Property(a => a.SourceContentHashSha256).HasMaxLength(80).IsRequired();

            builder.Property(a => a.ReceivedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(a => a.FinishedAtUtc).HasColumnType("datetime(6)");

            builder.Property(a => a.TotalRecordsInSource).IsRequired();
            builder.Property(a => a.RecordsSucceeded).IsRequired();
            builder.Property(a => a.RecordsFailed).IsRequired();

            builder.Property(a => a.LineageSerializedJson).HasMaxLength(4000);

            builder.HasIndex(a => new { a.RestaurantId, a.ReceivedAtUtc });

            builder.HasMany(a => a.Errors)
                .WithOne(e => e.Import)
                .HasForeignKey(e => e.ImportId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ImportErrorLog>(builder =>
        {
            builder.ToTable("import_error_logs");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();

            builder.Property(e => e.ImportId).IsRequired();
            builder.Property(e => e.SourceRowNumber).IsRequired();
            builder.Property(e => e.EntityKeyValue).HasMaxLength(255);
            builder.Property(e => e.ErrorMessage).HasMaxLength(500).IsRequired();
            builder.Property(e => e.RawRowPayloadJson).HasMaxLength(4000);
            builder.Property(e => e.CreatedAtUtc).HasColumnType("datetime(6)").IsRequired();

            builder.HasIndex(e => e.ImportId);
        });

        modelBuilder.Entity<TmpAuthorization>(builder =>
        {
            builder.ToTable("tmp_authorizations");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.RestaurantId).IsRequired();
            builder.Property(t => t.Name).HasMaxLength(120).IsRequired();
            builder.Property(t => t.TokenHash).HasMaxLength(512).IsRequired();
            builder.Property(t => t.Scope).HasMaxLength(80).IsRequired();
            builder.Property(t => t.CreatedByUserId);
            builder.Property(t => t.AutoRotateAtUtc).HasColumnType("datetime(6)");
            builder.Property(t => t.ExpiresAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(t => t.RevokedAtUtc).HasColumnType("datetime(6)");
            builder.Property(t => t.RevokedByUserId);
            builder.Property(t => t.LastUsedAtUtc).HasColumnType("datetime(6)");
            builder.Property(t => t.Description).HasMaxLength(500);
            builder.Property(t => t.CreatedAtUtc).HasColumnType("datetime(6)").IsRequired();
            builder.Property(t => t.UpdatedAtUtc).HasColumnType("datetime(6)");

            builder.HasIndex(t => new { t.RestaurantId, t.TokenHash }).IsUnique();
            builder.HasIndex(t => new { t.RestaurantId, t.ExpiresAtUtc });
            builder.HasIndex(t => t.TokenHash).IsUnique();

            builder.HasOne(t => t.Restaurant)
                .WithMany()
                .HasForeignKey(t => t.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.CreatedByUser)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(t => t.RevokedByUser)
                .WithMany()
                .HasForeignKey(t => t.RevokedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.ApplyTenantQueryFilter(this);
        });
    }
}

public static class TenantQueryFilterExtensions
{
    public static EntityTypeBuilder<TEntity> ApplyTenantQueryFilter<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        IsmDbContext dbContext)
        where TEntity : class
    {
        var param = Expression.Parameter(typeof(TEntity), "e");
        var restaurantIdProp = typeof(TEntity).GetProperty("RestaurantId",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (restaurantIdProp == null) return builder;

        var propertyAccess = Expression.Property(param, restaurantIdProp);
        var propertyNullable = Expression.Convert(propertyAccess, typeof(int?));

        var restaurantIdExpr = Expression.Convert(
            Expression.Property(Expression.Constant(dbContext), nameof(IsmDbContext.CurrentUserRestaurantId)),
            typeof(int?));
        var applyFilterExpr = Expression.Property(
            Expression.Constant(dbContext), nameof(IsmDbContext.ApplyTenantFilter));

        Expression condition = Expression.OrElse(
            Expression.Not(applyFilterExpr),
            Expression.Equal(propertyNullable, restaurantIdExpr));

        var lambda = Expression.Lambda<Func<TEntity, bool>>(condition, param);
        builder.HasQueryFilter(lambda);

        return builder;
    }
}
