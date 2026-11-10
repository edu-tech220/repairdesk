using System.ComponentModel.DataAnnotations;
using RepairDesk.Api.Models.Enums;

namespace RepairDesk.Api.Dtos;

public record DeviceDto(
    int Id,
    string SerialNumber,
    string ProductName,
    string OwnerName,
    string OwnerEmail,
    DateTime PurchaseDate,
    WarrantyTier WarrantyTier,
    DateTime WarrantyExpiresOn);

public record RegisterDeviceRequest(
    [Required] string SerialNumber,
    [Range(1, int.MaxValue)] int ProductId,
    [Required] string OwnerName,
    [Required, EmailAddress] string OwnerEmail,
    [Required] DateTime PurchaseDate,
    WarrantyTier WarrantyTier);
