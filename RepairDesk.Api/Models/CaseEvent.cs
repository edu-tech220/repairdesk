using RepairDesk.Api.Models.Enums;

namespace RepairDesk.Api.Models;

public class CaseEvent
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public RmaCase? Case { get; set; }

    public CaseStatus FromStatus { get; set; }
    public CaseStatus ToStatus { get; set; }
    public string? Note { get; set; }
    public string ActorUsername { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}
