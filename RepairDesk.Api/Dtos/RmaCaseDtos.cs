using System.ComponentModel.DataAnnotations;
using RepairDesk.Api.Models.Enums;

namespace RepairDesk.Api.Dtos;

public record SubmitClaimRequest(
    [Required] string SerialNumber,
    [Required, MinLength(5)] string ReportedIssue);

public record RejectRequest([Required] string Reason);

public record ReceiveRequest(string? TrackingNumber);

public record ResolveRequest(ResolutionType Resolution, string? Notes);

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

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
