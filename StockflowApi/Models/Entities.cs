namespace StockflowApi.Models;

public class Product
{
    public long Id { get; set; }
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "General";
    public string? Barcode { get; set; }
    public int Quantity { get; set; }
    public int ReorderLevel { get; set; } = 5;
    public decimal UnitPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class StockMovement
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public Product? Product { get; set; }
    public int Change { get; set; }
    public string Reason { get; set; } = "Manual adjustment";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Customer
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Supplier
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Sale
{
    public long Id { get; set; }
    public long? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string PaymentMethod { get; set; } = "cash";
    public string Note { get; set; } = "";
    public DateTime SoldAt { get; set; } = DateTime.UtcNow;
    public List<SaleItem> Items { get; set; } = new();
}

public class SaleItem
{
    public long Id { get; set; }
    public long SaleId { get; set; }
    public long ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}

public class Purchase
{
    public long Id { get; set; }
    public long? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string InvoiceNo { get; set; } = "";
    public decimal Total { get; set; }
    public string Note { get; set; } = "";
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
    public List<PurchaseItem> Items { get; set; } = new();
}

public class PurchaseItem
{
    public long Id { get; set; }
    public long PurchaseId { get; set; }
    public long ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Total { get; set; }
}

public class AppSettings
{
    public int Id { get; set; } = 1;
    public string ShopName { get; set; } = "My Shop";
    public string ShopAddress { get; set; } = "";
    public string ShopPhone { get; set; } = "";
    public string ReceiptNote { get; set; } = "";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class DomainException : Exception
{
    public int Status { get; }
    public DomainException(string message, int status = 400) : base(message) => Status = status;
}
