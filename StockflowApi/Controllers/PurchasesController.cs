using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockflowApi.Data;
using StockflowApi.Dtos;
using StockflowApi.Models;

namespace StockflowApi.Controllers;

[ApiController]
[Route("api/purchases")]
public class PurchasesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<PurchaseDto>> List()
    {
        var rows = await db.Purchases.AsNoTracking().Include(p => p.Supplier).Include(p => p.Items)
            .OrderByDescending(p => p.PurchasedAt).ThenByDescending(p => p.Id).Take(200).ToListAsync();
        return rows.Select(p => new PurchaseDto(p.Id, p.SupplierId, p.Supplier?.Name, p.InvoiceNo, p.Total,
            p.Note, p.PurchasedAt, p.Items.Count)).ToList();
    }

    [HttpGet("{id:long}")]
    public async Task<PurchaseDetailDto> Get(long id)
    {
        var p = await db.Purchases.AsNoTracking()
            .Include(x => x.Supplier).Include(x => x.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Purchase not found.", 404);
        return new PurchaseDetailDto(p.Id, p.SupplierId, p.Supplier?.Name, p.InvoiceNo, p.Total, p.Note,
            p.PurchasedAt, p.Items.Count,
            p.Items.Select(i => new PurchaseItemDto(i.Id, i.ProductId, i.Product?.Name ?? "Unknown product",
                i.Product?.Sku ?? "", i.Quantity, i.UnitCost, i.Total)).ToList());
    }

    [HttpPost]
    public async Task<object> Create(PurchaseInput input)
    {
        if (input.SupplierId.HasValue && !await db.Suppliers.AnyAsync(s => s.Id == input.SupplierId))
            throw new DomainException("Supplier not found.");

        await using var tx = await db.Database.BeginTransactionAsync();
        var ids = input.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        var lines = new List<PurchaseItem>();
        foreach (var item in input.Items)
        {
            if (!products.ContainsKey(item.ProductId))
                throw new DomainException("A product in this purchase no longer exists.");
            lines.Add(new PurchaseItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                Total = item.UnitCost * item.Quantity,
            });
        }

        var invoice = input.InvoiceNo.Trim();
        var purchase = new Purchase
        {
            SupplierId = input.SupplierId,
            InvoiceNo = invoice,
            Total = lines.Sum(l => l.Total),
            Note = input.Note.Trim(),
            Items = lines,
        };
        db.Purchases.Add(purchase);

        foreach (var l in lines)
        {
            var p = products[l.ProductId];
            p.Quantity += l.Quantity;
            p.UpdatedAt = DateTime.UtcNow;
            db.StockMovements.Add(new StockMovement
            {
                ProductId = p.Id,
                Change = l.Quantity,
                Reason = invoice.Length > 0 ? $"Purchase (invoice {invoice})" : "Purchase",
            });
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return new { id = purchase.Id };
    }
}
