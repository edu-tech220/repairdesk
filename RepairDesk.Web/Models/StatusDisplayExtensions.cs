namespace RepairDesk.Web.Models;

public static class StatusDisplayExtensions
{
    public static string CssBadgeClass(this CaseStatus status) => status switch
    {
        CaseStatus.UnderReview => "badge-review",
        CaseStatus.AwaitingReturn => "badge-awaiting",
        CaseStatus.Diagnosing => "badge-diagnosing",
        CaseStatus.Backordered => "badge-backordered",
        CaseStatus.Resolved => "badge-resolved",
        CaseStatus.Closed => "badge-closed",
        CaseStatus.Rejected => "badge-rejected",
        _ => "badge"
    };
}
