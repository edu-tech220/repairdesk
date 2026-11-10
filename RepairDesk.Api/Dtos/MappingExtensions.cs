using RepairDesk.Api.Common;
using RepairDesk.Api.Models;

namespace RepairDesk.Api.Dtos;

public static class MappingExtensions
{
    public static ProductDto ToDto(this Product product) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.Category,
        product.Price,
        product.StandardWarrantyMonths,
        product.ExtendedWarrantyMonths,
        product.Inventory?.QuantityOnHand ?? 0);

    public static DeviceDto ToDto(this RegisteredDevice device) => new(
        device.Id,
        device.SerialNumber,
        device.Product?.Name ?? string.Empty,
        device.OwnerName,
        device.OwnerEmail,
        device.PurchaseDate,
        device.WarrantyTier,
        device.WarrantyExpiresOn);

    public static RmaCaseSummaryDto ToSummaryDto(this RmaCase rmaCase) => new(
        rmaCase.Id,
        rmaCase.Device?.SerialNumber ?? string.Empty,
        rmaCase.Device?.Product?.Name ?? string.Empty,
        rmaCase.Status,
        rmaCase.CreatedAt);

    public static RmaCaseDetailDto ToDetailDto(this RmaCase rmaCase) => new(
        rmaCase.Id,
        rmaCase.Device?.SerialNumber ?? string.Empty,
        rmaCase.Device?.Product?.Name ?? string.Empty,
        rmaCase.ReportedIssue,
        rmaCase.Status,
        rmaCase.Resolution,
        rmaCase.ResolutionNotes,
        rmaCase.RefundAmount,
        rmaCase.TrackingNumber,
        rmaCase.DenialReason,
        rmaCase.CreatedAt,
        rmaCase.AwaitingReturnAt,
        rmaCase.ReceivedAt,
        rmaCase.ResolvedAt,
        rmaCase.ClosedAt,
        ETag.ToHeaderValue(rmaCase.RowVersion));

    public static CaseEventDto ToDto(this CaseEvent caseEvent) => new(
        caseEvent.FromStatus,
        caseEvent.ToStatus,
        caseEvent.Note,
        caseEvent.ActorUsername,
        caseEvent.OccurredAt);
}
