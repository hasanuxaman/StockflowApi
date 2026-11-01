using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockflowApi.Data;
using StockflowApi.Dtos;
using StockflowApi.Models;

namespace StockflowApi.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<CustomerDto>> List() =>
        await db.Customers.AsNoTracking().OrderBy(c => c.Name)
            .Select(c => new CustomerDto(c.Id, c.Name, c.Phone, c.Address, c.CreatedAt,
                db.Sales.Count(s => s.CustomerId == c.Id)))
            .ToListAsync();

    [HttpPost]
    public async Task<CustomerDto> Create(PartnerInput input)
    {
        var c = new Customer { Name = input.Name.Trim(), Phone = input.Phone.Trim(), Address = input.Address.Trim() };
        db.Customers.Add(c);
        await db.SaveChangesAsync();
        return new CustomerDto(c.Id, c.Name, c.Phone, c.Address, c.CreatedAt, 0);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, PartnerInput input)
    {
        var c = await db.Customers.FindAsync(id) ?? throw new DomainException("Customer not found.", 404);
        c.Name = input.Name.Trim(); c.Phone = input.Phone.Trim(); c.Address = input.Address.Trim();
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var c = await db.Customers.FindAsync(id) ?? throw new DomainException("Customer not found.", 404);
        // Past sales keep their history; the customer link is cleared.
        var sales = await db.Sales.Where(s => s.CustomerId == id).ToListAsync();
        foreach (var s in sales) s.CustomerId = null;
        db.Customers.Remove(c);
        await db.SaveChangesAsync();
        return NoContent();
    }
}

[ApiController]
[Route("api/suppliers")]
public class SuppliersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<SupplierDto>> List() =>
        await db.Suppliers.AsNoTracking().OrderBy(s => s.Name)
            .Select(s => new SupplierDto(s.Id, s.Name, s.Phone, s.Address, s.CreatedAt,
                db.Purchases.Count(p => p.SupplierId == s.Id)))
            .ToListAsync();

    [HttpPost]
    public async Task<SupplierDto> Create(PartnerInput input)
    {
        var s = new Supplier { Name = input.Name.Trim(), Phone = input.Phone.Trim(), Address = input.Address.Trim() };
        db.Suppliers.Add(s);
        await db.SaveChangesAsync();
        return new SupplierDto(s.Id, s.Name, s.Phone, s.Address, s.CreatedAt, 0);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, PartnerInput input)
    {
        var s = await db.Suppliers.FindAsync(id) ?? throw new DomainException("Supplier not found.", 404);
        s.Name = input.Name.Trim(); s.Phone = input.Phone.Trim(); s.Address = input.Address.Trim();
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var s = await db.Suppliers.FindAsync(id) ?? throw new DomainException("Supplier not found.", 404);
        var purchases = await db.Purchases.Where(p => p.SupplierId == id).ToListAsync();
        foreach (var p in purchases) p.SupplierId = null;
        db.Suppliers.Remove(s);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
