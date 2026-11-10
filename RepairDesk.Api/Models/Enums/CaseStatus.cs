namespace RepairDesk.Api.Models.Enums;

/// <summary>
/// UnderReview -> AwaitingReturn -> Diagnosing -> Resolved -> Closed is the happy path.
/// UnderReview can also go straight to Rejected (expired warranty or denied claim).
/// Diagnosing can go to Backordered when a Replace resolution has no stock, until replenished.
/// </summary>
public enum CaseStatus
{
    UnderReview = 0,
    Rejected = 1,
    AwaitingReturn = 2,
    Diagnosing = 3,
    Backordered = 4,
    Resolved = 5,
    Closed = 6
}
