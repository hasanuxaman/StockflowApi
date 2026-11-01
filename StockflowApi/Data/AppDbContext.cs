using Microsoft.EntityFrameworkCore;
using StockflowApi.Models;

namespace StockflowApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<AppSettings> Settings => Set<AppSettings>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Product>(e =>
        {
            e.HasIndex(p => p.Sku).IsUnique();
            e.HasIndex(p => p.Barcode);
            e.Property(p => p.Sku).HasMaxLength(40);
            e.Property(p => p.Name).HasMaxLength(120);
            e.Property(p => p.Category).HasMaxLength(60);
            e.Property(p => p.UnitPrice).HasPrecision(12, 2);
        });

        b.Entity<StockMovement>(e =>
        {
            e.HasIndex(m => new { m.ProductId, m.CreatedAt });
            e.HasOne(m => m.Product).WithMany().HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Sale>(e =>
        {
            e.HasIndex(s => s.SoldAt);
            e.Property(s => s.Subtotal).HasPrecision(12, 2);
            e.Property(s => s.Discount).HasPrecision(12, 2);
            e.Property(s => s.Total).HasPrecision(12, 2);
            e.HasOne(s => s.Customer).WithMany().HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(s => s.Items).WithOne().HasForeignKey(i => i.SaleId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<SaleItem>(e =>
        {
            e.Property(i => i.UnitPrice).HasPrecision(12, 2);
            e.Property(i => i.Total).HasPrecision(12, 2);
            e.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Purchase>(e =>
        {
            e.HasIndex(p => p.PurchasedAt);
            e.Property(p => p.Total).HasPrecision(12, 2);
            e.HasOne(p => p.Supplier).WithMany().HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(p => p.Items).WithOne().HasForeignKey(i => i.PurchaseId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PurchaseItem>(e =>
        {
            e.Property(i => i.UnitCost).HasPrecision(12, 2);
            e.Property(i => i.Total).HasPrecision(12, 2);
            e.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<AppSettings>().Property(s => s.Id).ValueGeneratedNever();
    }

    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Settings.AnyAsync())
            db.Settings.Add(new AppSettings { Id = 1, ShopName = "My Shop" });

        if (!await db.Products.AnyAsync())
        {
            var seed = new[]
            {
                new Product { Sku = "BLT-M8-50", Name = "M8 Hex Bolt (box of 50)", Category = "Fasteners", Quantity = 120, ReorderLevel = 30, UnitPrice = 6.50m },
                new Product { Sku = "GLV-L-NIT", Name = "Nitrile Gloves, Large", Category = "Safety", Quantity = 8, ReorderLevel = 15, UnitPrice = 12.00m },
                new Product { Sku = "TPE-48-CLR", Name = "Packing Tape 48mm Clear", Category = "Packaging", Quantity = 64, ReorderLevel = 20, UnitPrice = 2.25m },
            };
            db.Products.AddRange(seed);
            await db.SaveChangesAsync();
            db.StockMovements.AddRange(seed.Select(p => new StockMovement { ProductId = p.Id, Change = p.Quantity, Reason = "Initial stock" }));
        }
        await db.SaveChangesAsync();
    }
}
