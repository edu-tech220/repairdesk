namespace RepairDesk.Web.Models;

public record LoginResponse(string Token, string Username, string Role);

public record ProductDto(
    int Id,
    string Sku,
    string Name,
    string Category,
    decimal Price,
    int StandardWarrantyMonths,
    int ExtendedWarrantyMonths,
    int QuantityOnHand);

public record DeviceDto(
    int Id,
    string SerialNumber,
    string ProductName,
    string OwnerName,
    string OwnerEmail,
    DateTime PurchaseDate,
    WarrantyTier WarrantyTier,
    DateTime WarrantyExpiresOn);

public record RmaCaseSummaryDto(
    int Id,
    string SerialNumber,
    string ProductName,
    CaseStatus Status,
    DateTime CreatedAt);

public record RmaCaseDetailDto(
    int Id,
    string SerialNumber,
    string ProductName,
    string ReportedIssue,
    CaseStatus Status,
    ResolutionType? Resolution,
    string? ResolutionNotes,
    decimal? RefundAmount,
    string? TrackingNumber,
    string? DenialReason,
    DateTime CreatedAt,
    DateTime? AwaitingReturnAt,
    DateTime? ReceivedAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    string ETag);

public record CaseEventDto(
    CaseStatus FromStatus,
    CaseStatus ToStatus,
    string? Note,
    string ActorUsername,
    DateTime OccurredAt);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public record ApiProblem(string? Title, string? Detail, int? Status);
