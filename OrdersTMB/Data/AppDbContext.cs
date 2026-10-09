using Microsoft.EntityFrameworkCore;
using OrdersTMB.Models;

namespace OrdersTMB.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<LogAudit> LogsAudit { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LogAudit>()
            .HasIndex(log => new { log.Table, log.EntityId, log.CreateDate });

        modelBuilder.Entity<OrderItem>()
            .HasOne<Order>()
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId);

        base.OnModelCreating(modelBuilder);
    }
}
