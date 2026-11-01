using System.ComponentModel.DataAnnotations;
using StockflowApi.Models;

namespace StockflowApi.Dtos;

// ---------- Products ----------
public class ProductInput
{
    [Required(ErrorMessage = "SKU is required"), StringLength(40)] public string Sku { get; set; } = "";
    [Required(ErrorMessage = "Name is required"), StringLength(120)] public string Name { get; set; } = "";
    [StringLength(60)] public string Category { get; set; } = "General";
    [StringLength(60)] public string? Barcode { get; set; }
    [Range(0, int.MaxValue)] public int Quantity { get; set; }
    [Range(0, int.MaxValue)] public int ReorderLevel { get; set; } = 5;
    [Range(0, 1_000_000)] public decimal UnitPrice { get; set; }
}

public class AdjustInput
{
    [Range(-1_000_000, 1_000_000)] public int Change { get; set; }
    [StringLength(200)] public string? Reason { get; set; }
}

public record ProductDto(long Id, string Sku, string Name, string Category, string? Barcode,
    int Quantity, int ReorderLevel, decimal UnitPrice, DateTime UpdatedAt)
{
    public static ProductDto From(Product p) =>
        new(p.Id, p.Sku, p.Name, p.Category, p.Barcode, p.Quantity, p.ReorderLevel, p.UnitPrice, p.UpdatedAt);
}

public record MovementDto(long Id, int Change, string Reason, DateTime CreatedAt);
public record InventorySummary(int TotalProducts, long TotalUnits, decimal StockValue, int LowStock);
public record InventoryDto(List<ProductDto> Products, InventorySummary Summary);

// ---------- Partners ----------
public class PartnerInput
{
    [Required(ErrorMessage = "Name is required"), StringLength(120)] public string Name { get; set; } = "";
    [StringLength(40)] public string Phone { get; set; } = "";
    [StringLength(200)] public string Address { get; set; } = "";
}

public record CustomerDto(long Id, string Name, string Phone, string Address, DateTime CreatedAt, int SalesCount);
public record SupplierDto(long Id, string Name, string Phone, string Address, DateTime CreatedAt, int PurchaseCount);

// ---------- Sales ----------
public class SaleLineInput
{
    [Range(1, long.MaxValue)] public long ProductId { get; set; }
    [Range(1, 100_000)] public int Quantity { get; set; }
}

public class SaleInput
{
    public long? CustomerId { get; set; }
    [Range(0, 1_000_000)] public decimal Discount { get; set; }
    [Required] public string PaymentMethod { get; set; } = "cash";
    [StringLength(300)] public string Note { get; set; } = "";
    [Required, MinLength(1, ErrorMessage = "Add at least one item to the sale.")]
    public List<SaleLineInput> Items { get; set; } = new();
}

public record SaleItemDto(long Id, long ProductId, string Name, string Sku, int Quantity, decimal UnitPrice, decimal Total);

public record SaleDto(long Id, string ReceiptNo, long? CustomerId, string? CustomerName, decimal Subtotal,
    decimal Discount, decimal Total, string PaymentMethod, string Note, DateTime SoldAt, int ItemCount);

public record SaleDetailDto(long Id, string ReceiptNo, long? CustomerId, string? CustomerName, decimal Subtotal,
    decimal Discount, decimal Total, string PaymentMethod, string Note, DateTime SoldAt, int ItemCount,
    List<SaleItemDto> Items);

// ---------- Purchases ----------
public class PurchaseLineInput
{
    [Range(1, long.MaxValue)] public long ProductId { get; set; }
    [Range(1, 1_000_000)] public int Quantity { get; set; }
    [Range(0, 10_000_000)] public decimal UnitCost { get; set; }
}

public class PurchaseInput
{
    public long? SupplierId { get; set; }
    [StringLength(60)] public string InvoiceNo { get; set; } = "";
    [StringLength(300)] public string Note { get; set; } = "";
    [Required, MinLength(1, ErrorMessage = "Add at least one item to the purchase.")]
    public List<PurchaseLineInput> Items { get; set; } = new();
}

public record PurchaseItemDto(long Id, long ProductId, string Name, string Sku, int Quantity, decimal UnitCost, decimal Total);

public record PurchaseDto(long Id, long? SupplierId, string? SupplierName, string InvoiceNo, decimal Total,
    string Note, DateTime PurchasedAt, int ItemCount);

public record PurchaseDetailDto(long Id, long? SupplierId, string? SupplierName, string InvoiceNo, decimal Total,
    string Note, DateTime PurchasedAt, int ItemCount, List<PurchaseItemDto> Items);

// ---------- Dashboard / Settings ----------
public record LowStockDto(long Id, string Name, int Quantity, int ReorderLevel);
public record TopProductDto(string Name, int Quantity, decimal Total);

public record DashboardDto(decimal TodayTotal, int TodayCount, decimal MonthTotal, decimal StockValue,
    int TotalProducts, long TotalUnits, List<LowStockDto> LowStock, List<TopProductDto> TopProducts,
    List<SaleDto> RecentSales);

public class SettingsInput
{
    [Required(ErrorMessage = "Shop name is required"), StringLength(80)] public string ShopName { get; set; } = "";
    [StringLength(200)] public string ShopAddress { get; set; } = "";
    [StringLength(40)] public string ShopPhone { get; set; } = "";
    [StringLength(200)] public string ReceiptNote { get; set; } = "";
}

public record SettingsDto(string ShopName, string ShopAddress, string ShopPhone, string ReceiptNote);
