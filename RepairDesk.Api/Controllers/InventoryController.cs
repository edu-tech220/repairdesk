using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RepairDesk.Api.Data;
using RepairDesk.Api.Dtos;
using RepairDesk.Api.Services;

namespace RepairDesk.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly RepairDeskDbContext _db;
    private readonly IInventoryService _inventory;

    public InventoryController(RepairDeskDbContext db, IInventoryService inventory)
    {
        _db = db;
        _inventory = inventory;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(CancellationToken cancellationToken)
    {
        var products = await _db.Products
            .Include(p => p.Inventory)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return Ok(products.Select(p => p.ToDto()));
    }

    [HttpPost("{productId:int}/restock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Restock(int productId, RestockRequest request, CancellationToken cancellationToken)
    {
        await _inventory.RestockAsync(productId, request.Quantity, cancellationToken);
        return NoContent();
    }
}
