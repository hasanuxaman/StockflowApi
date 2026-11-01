using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockflowApi.Data;
using StockflowApi.Dtos;
using StockflowApi.Models;

namespace StockflowApi.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(AppDbContext db) : ControllerBase
{
    // The shop runs on Bangladesh time (UTC+6, no DST).
    private static DateOnly Day(DateTime utc) =>
        DateOnly.FromDateTime(DateTime.SpecifyKind(utc, DateTimeKind.Utc).AddHours(6));

    [HttpGet]
    public async Task<DashboardDto> Get()
    {
        var since = DateTime.UtcNow.AddDays(-60);
        var products = await db.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
        var sales = await db.Sales.AsNoTracking().Include(s => s.Customer).Include(s => s.Items)
            .ThenInclude(i => i.Product)
            .Where(s => s.SoldAt >= since)
            .OrderByDescending(s => s.SoldAt).ThenByDescending(s => s.Id)
            .Take(500).ToListAsync();

        var today = Day(DateTime.UtcNow);
        var todaySales = sales.Where(s => Day(s.SoldAt) == today).ToList();
        var monthSales = sales.Where(s => Day(s.SoldAt).Year == today.Year && Day(s.SoldAt).Month == today.Month).ToList();

        var cutoff = Day(DateTime.UtcNow.AddDays(-30));
        var top = sales.Where(s => Day(s.SoldAt) >= cutoff)
            .SelectMany(s => s.Items)
            .GroupBy(i => i.Product?.Name ?? "Unknown")
            .Select(g => new TopProductDto(g.Key, g.Sum(i => i.Quantity), g.Sum(i => i.Total)))
            .OrderByDescending(t => t.Total).Take(5).ToList();

        return new DashboardDto(
            todaySales.Sum(s => s.Total),
            todaySales.Count,
            monthSales.Sum(s => s.Total),
            products.Sum(p => p.Quantity * p.UnitPrice),
            products.Count,
            products.Sum(p => (long)p.Quantity),
            products.Where(p => p.Quantity <= p.ReorderLevel)
                .Select(p => new LowStockDto(p.Id, p.Name, p.Quantity, p.ReorderLevel)).Take(8).ToList(),
            top,
            sales.Take(6).Select(s => new SaleDto(s.Id, SalesController.ReceiptNo(s.Id), s.CustomerId,
                s.Customer?.Name, s.Subtotal, s.Discount, s.Total, s.PaymentMethod, s.Note, s.SoldAt,
                s.Items.Count)).ToList());
    }
}

[ApiController]
[Route("api/settings")]
public class SettingsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<SettingsDto> Get()
    {
        var s = await db.Settings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1) ?? new AppSettings();
        return new SettingsDto(s.ShopName, s.ShopAddress, s.ShopPhone, s.ReceiptNote);
    }

    [HttpPut]
    public async Task<IActionResult> Save(SettingsInput input)
    {
        var s = await db.Settings.FirstOrDefaultAsync(x => x.Id == 1);
        if (s is null) { s = new AppSettings { Id = 1 }; db.Settings.Add(s); }
        s.ShopName = input.ShopName.Trim();
        s.ShopAddress = input.ShopAddress.Trim();
        s.ShopPhone = input.ShopPhone.Trim();
        s.ReceiptNote = input.ReceiptNote.Trim();
        s.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
