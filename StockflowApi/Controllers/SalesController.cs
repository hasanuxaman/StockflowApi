using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockflowApi.Data;
using StockflowApi.Dtos;
using StockflowApi.Models;

namespace StockflowApi.Controllers;

[ApiController]
[Route("api/sales")]
public class SalesController(AppDbContext db) : ControllerBase
{
    public static readonly string[] PaymentMethods = { "cash", "card", "bkash", "nagad" };
    public static string ReceiptNo(long id) => $"R-{id:D6}";

    [HttpGet]
    public async Task<List<SaleDto>> List([FromQuery] long? customerId)
    {
        var q = db.Sales.AsNoTracking().Include(s => s.Customer).Include(s => s.Items).AsQueryable();
        if (customerId.HasValue) q = q.Where(s => s.CustomerId == customerId);
        var rows = await q.OrderByDescending(s => s.SoldAt).ThenByDescending(s => s.Id).Take(200).ToListAsync();
        return rows.Select(ToDto).ToList();
    }

    [HttpGet("{id:long}")]
    public async Task<SaleDetailDto> Get(long id)
    {
        var s = await db.Sales.AsNoTracking()
            .Include(x => x.Customer).Include(x => x.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Sale not found.", 404);
        return new SaleDetailDto(s.Id, ReceiptNo(s.Id), s.CustomerId, s.Customer?.Name, s.Subtotal, s.Discount,
            s.Total, s.PaymentMethod, s.Note, s.SoldAt, s.Items.Count,
            s.Items.Select(i => new SaleItemDto(i.Id, i.ProductId, i.Product?.Name ?? "Unknown product",
                i.Product?.Sku ?? "", i.Quantity, i.UnitPrice, i.Total)).ToList());
    }

    [HttpPost]
    public async Task<object> Create(SaleInput input)
    {
        var method = input.PaymentMethod.Trim().ToLower();
        if (!PaymentMethods.Contains(method)) throw new DomainException("Unknown payment method.");
        if (input.CustomerId.HasValue && !await db.Customers.AnyAsync(c => c.Id == input.CustomerId))
            throw new DomainException("Customer not found.");

        // Merge duplicate lines so stock is checked against the total requested.
        var wanted = input.Items.GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        await using var tx = await db.Database.BeginTransactionAsync();
        var ids = wanted.Keys.ToList();
        var products = await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        var lines = new List<SaleItem>();
        foreach (var (productId, qty) in wanted)
        {
            if (!products.TryGetValue(productId, out var p))
                throw new DomainException("A product in the cart no longer exists.");
            if (p.Quantity < qty)
                throw new DomainException($"Not enough stock: {p.Name} has {p.Quantity} on hand.");
            lines.Add(new SaleItem { ProductId = productId, Quantity = qty, UnitPrice = p.UnitPrice, Total = p.UnitPrice * qty });
        }

        var subtotal = lines.Sum(l => l.Total);
        var discount = Math.Min(input.Discount, subtotal);
        var sale = new Sale
        {
            CustomerId = input.CustomerId,
            Subtotal = subtotal,
            Discount = discount,
            Total = subtotal - discount,
            PaymentMethod = method,
            Note = input.Note.Trim(),
            Items = lines,
        };
        db.Sales.Add(sale);
        await db.SaveChangesAsync();

        foreach (var l in lines)
        {
            var p = products[l.ProductId];
            p.Quantity -= l.Quantity;
            p.UpdatedAt = DateTime.UtcNow;
            db.StockMovements.Add(new StockMovement
            {
                ProductId = p.Id,
                Change = -l.Quantity,
                Reason = $"Sale {ReceiptNo(sale.Id)}",
            });
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return new { id = sale.Id, receiptNo = ReceiptNo(sale.Id) };
    }

    private static SaleDto ToDto(Sale s) =>
        new(s.Id, ReceiptNo(s.Id), s.CustomerId, s.Customer?.Name, s.Subtotal, s.Discount, s.Total,
            s.PaymentMethod, s.Note, s.SoldAt, s.Items.Count);
}
