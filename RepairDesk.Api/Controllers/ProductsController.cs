using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RepairDesk.Api.Data;
using RepairDesk.Api.Dtos;
using RepairDesk.Api.Exceptions;
using RepairDesk.Api.Models;

namespace RepairDesk.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly RepairDeskDbContext _db;

    public ProductsController(RepairDeskDbContext db)
    {
        _db = db;
    }

    /// <summary>List every product with its current stock level.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(CancellationToken cancellationToken)
    {
        var products = await _db.Products
            .Include(p => p.Inventory)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return Ok(products.Select(p => p.ToDto()));
    }

    /// <summary>Get a single product by id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException(nameof(Product), id);
        }

        return Ok(product.ToDto());
    }

    /// <summary>Create a product with its warranty terms and initial stock. Admin only.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Sku = request.Sku,
            Name = request.Name,
            Category = request.Category,
            Price = request.Price,
            StandardWarrantyMonths = request.StandardWarrantyMonths,
            ExtendedWarrantyMonths = request.ExtendedWarrantyMonths
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);

        _db.InventoryItems.Add(new InventoryItem { ProductId = product.Id, QuantityOnHand = request.InitialQuantity });
        await _db.SaveChangesAsync(cancellationToken);

        product.Inventory = await _db.InventoryItems.FirstAsync(i => i.ProductId == product.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product.ToDto());
    }
}
