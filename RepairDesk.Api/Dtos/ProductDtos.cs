using System.ComponentModel.DataAnnotations;

namespace RepairDesk.Api.Dtos;

public record ProductDto(
    int Id,
    string Sku,
    string Name,
    string Category,
    decimal Price,
    int StandardWarrantyMonths,
    int ExtendedWarrantyMonths,
    int QuantityOnHand);

public record CreateProductRequest(
    [Required] string Sku,
    [Required] string Name,
    [Required] string Category,
    [Range(0.01, double.MaxValue)] decimal Price,
    [Range(1, 120)] int StandardWarrantyMonths,
    [Range(1, 120)] int ExtendedWarrantyMonths,
    [Range(0, int.MaxValue)] int InitialQuantity);

public record RestockRequest([Range(1, int.MaxValue)] int Quantity);
