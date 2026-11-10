using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RepairDesk.Api.Data;
using RepairDesk.Api.Dtos;
using RepairDesk.Api.Exceptions;
using RepairDesk.Api.Models;
using RepairDesk.Api.Services;

namespace RepairDesk.Api.Controllers;

[ApiController]
[Route("api/devices")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly RepairDeskDbContext _db;

    public DevicesController(RepairDeskDbContext db)
    {
        _db = db;
    }

    /// <summary>Look up a registered device by serial number.</summary>
    [HttpGet("{serialNumber}")]
    public async Task<ActionResult<DeviceDto>> GetBySerialNumber(string serialNumber, CancellationToken cancellationToken)
    {
        var device = await _db.Devices
            .Include(d => d.Product)
            .FirstOrDefaultAsync(d => d.SerialNumber == serialNumber, cancellationToken);

        if (device is null)
        {
            throw new NotFoundException(nameof(RegisteredDevice), serialNumber);
        }

        return Ok(device.ToDto());
    }

    /// <summary>Register a purchased device, computing its warranty expiration from the product's warranty terms.</summary>
    [HttpPost]
    public async Task<ActionResult<DeviceDto>> Register(RegisterDeviceRequest request, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException(nameof(Product), request.ProductId);
        }

        var device = new RegisteredDevice
        {
            SerialNumber = request.SerialNumber,
            ProductId = product.Id,
            OwnerName = request.OwnerName,
            OwnerEmail = request.OwnerEmail,
            PurchaseDate = request.PurchaseDate,
            WarrantyTier = request.WarrantyTier,
            WarrantyExpiresOn = WarrantyCalculator.CalculateExpiration(product, request.PurchaseDate, request.WarrantyTier)
        };

        _db.Devices.Add(device);
        await _db.SaveChangesAsync(cancellationToken);

        device.Product = product;
        return CreatedAtAction(nameof(GetBySerialNumber), new { serialNumber = device.SerialNumber }, device.ToDto());
    }
}
