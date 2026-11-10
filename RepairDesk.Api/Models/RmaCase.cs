using RepairDesk.Api.Models.Enums;

namespace RepairDesk.Api.Models;

public class RmaCase : IHasRowVersion
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public RegisteredDevice? Device { get; set; }

    public string ReportedIssue { get; set; } = string.Empty;
    public CaseStatus Status { get; set; } = CaseStatus.UnderReview;

    public ResolutionType? Resolution { get; set; }
    public string? ResolutionNotes { get; set; }
    public decimal? RefundAmount { get; set; }
    public string? TrackingNumber { get; set; }
    public string? DenialReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? AwaitingReturnAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public ICollection<CaseEvent> Events { get; set; } = new List<CaseEvent>();

    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();
}
