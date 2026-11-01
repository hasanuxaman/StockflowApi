using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockflowApi.Data;
using StockflowApi.Dtos;
using StockflowApi.Models;

namespace StockflowApi.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<InventoryDto> List([FromQuery] string? search)
    {
        var q = db.Products.AsNoTracking().AsQueryable();
        var term = (search ?? "").Replace("%", "").Trim().ToLower();
        if (term.Length > 0)
            q = q.Where(p => p.Name.ToLower().Contains(term) || p.Sku.ToLower().Contains(term)
                          || p.Category.ToLower().Contains(term)
                          || (p.Barcode != null && p.Barcode.ToLower().Contains(term)));

        var rows = await q.OrderBy(p => p.Name).ToListAsync();
        var products = rows.Select(ProductDto.From).ToList();
        var summary = new InventorySummary(
            products.Count,
            products.Sum(p => (long)p.Quantity),
            products.Sum(p => p.Quantity * p.UnitPrice),
            products.Count(p => p.Quantity <= p.ReorderLevel));
        return new InventoryDto(products, summary);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ProductDto>> Get(long id)
    {
        var p = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return p is null ? throw new DomainException("Product not found.", 404) : ProductDto.From(p);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(ProductInput input)
    {
        var sku = input.Sku.Trim();
        if (await db.Products.AnyAsync(p => p.Sku == sku))
            throw new DomainException($"SKU '{sku}' already exists.");

        await using var tx = await db.Database.BeginTransactionAsync();
        var product = new Product
        {
            Sku = sku,
            Name = input.Name.Trim(),
            Category = string.IsNullOrWhiteSpace(input.Category) ? "General" : input.Category.Trim(),
            Barcode = string.IsNullOrWhiteSpace(input.Barcode) ? null : input.Barcode.Trim(),
            Quantity = input.Quantity,
            ReorderLevel = input.ReorderLevel,
            UnitPrice = input.UnitPrice,
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        if (product.Quantity > 0)
            db.StockMovements.Add(new StockMovement { ProductId = product.Id, Change = product.Quantity, Reason = "Initial stock" });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return CreatedAtAction(nameof(Get), new { id = product.Id }, ProductDto.From(product));
    }

    [HttpPut("{id:long}")]
    public async Task<ProductDto> Update(long id, ProductInput input)
    {
        var product = await db.Products.FindAsync(id) ?? throw new DomainException("Product not found.", 404);
        var sku = input.Sku.Trim();
        if (await db.Products.AnyAsync(p => p.Sku == sku && p.Id != id))
            throw new DomainException($"SKU '{sku}' already exists.");

        product.Sku = sku;
        product.Name = input.Name.Trim();
        product.Category = string.IsNullOrWhiteSpace(input.Category) ? "General" : input.Category.Trim();
        product.Barcode = string.IsNullOrWhiteSpace(input.Barcode) ? null : input.Barcode.Trim();
        product.ReorderLevel = input.ReorderLevel;
        product.UnitPrice = input.UnitPrice;
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ProductDto.From(product);
    }

    [HttpPost("{id:long}/adjust")]
    public async Task<ProductDto> Adjust(long id, AdjustInput input)
    {
        if (input.Change == 0) throw new DomainException("Change must be non-zero.");
        await using var tx = await db.Database.BeginTransactionAsync();
        var product = await db.Products.FindAsync(id) ?? throw new DomainException("Product not found.", 404);
        if (product.Quantity + input.Change < 0)
            throw new DomainException($"Not enough stock: {product.Name} has {product.Quantity} on hand.");

        product.Quantity += input.Change;
        product.UpdatedAt = DateTime.UtcNow;
        db.StockMovements.Add(new StockMovement
        {
            ProductId = id,
            Change = input.Change,
            Reason = string.IsNullOrWhiteSpace(input.Reason) ? "Manual adjustment" : input.Reason.Trim(),
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return ProductDto.From(product);
    }

    [HttpGet("{id:long}/movements")]
    public async Task<List<MovementDto>> Movements(long id)
    {
        var rows = await db.StockMovements.AsNoTracking()
            .Where(m => m.ProductId == id)
            .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
            .Take(50).ToListAsync();
        return rows.Select(m => new MovementDto(m.Id, m.Change, m.Reason, m.CreatedAt)).ToList();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var product = await db.Products.FindAsync(id) ?? throw new DomainException("Product not found.", 404);
        db.Products.Remove(product);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            throw new DomainException($"{product.Name} is used in sales or purchases and can't be deleted.", 409);
        }
        return NoContent();
    }
}
