using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nexora.Web.Data.Entities;
using Nexora.Web.Data.Models;

namespace Nexora.Web.Data;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Organization>().ToTable("Organizations");
        b.Entity<AppUser>().ToTable("Users");
        b.Entity<SubscriptionPlan>().ToTable("SubscriptionPlans");
        b.Entity<Subscription>().ToTable("Subscriptions");
        b.Entity<Category>().ToTable("Categories");
        b.Entity<Product>().ToTable("Products");
        b.Entity<Customer>().ToTable("Customers");
        b.Entity<Order>().ToTable("Orders");
        b.Entity<OrderItem>().ToTable("OrderItems");
        b.Entity<StockMovement>().ToTable("StockMovements");
        b.Entity<ContactMessage>().ToTable("ContactMessages");

        b.Entity<Organization>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<SubscriptionPlan>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<Subscription>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<Category>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<Product>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<Customer>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<Order>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<OrderItem>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<StockMovement>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<ContactMessage>().HasQueryFilter(x => !x.IsDeleted);

        b.Entity<AppUser>()
            .HasOne(x => x.Organization)
            .WithMany(o => o.Users)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Subscription>()
            .HasOne(x => x.Organization)
            .WithMany(o => o.Subscriptions)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Category>()
            .HasOne(x => x.Organization)
            .WithMany(o => o.Categories)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Product>()
            .HasOne(x => x.Organization)
            .WithMany(o => o.Products)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Customer>()
            .HasOne(x => x.Organization)
            .WithMany(o => o.Customers)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Order>()
            .HasOne(x => x.Organization)
            .WithMany(o => o.Orders)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<OrderItem>()
            .HasOne(x => x.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<OrderItem>()
            .HasOne(x => x.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<StockMovement>()
            .HasOne(x => x.Organization)
            .WithMany(o => o.StockMovements)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<StockMovement>()
            .HasOne(x => x.Product)
            .WithMany(p => p.StockMovements)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<StockMovement>()
            .HasOne(x => x.RelatedOrder)
            .WithMany()
            .HasForeignKey(x => x.RelatedOrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    public override int SaveChanges()
    {
        ApplyAudit();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAudit();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAudit()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = utcNow;
                entry.Entity.UpdatedAtUtc = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = utcNow;
            }
        }
    }
}
