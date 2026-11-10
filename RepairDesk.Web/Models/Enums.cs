namespace RepairDesk.Web.Models;

public enum WarrantyTier
{
    Standard = 0,
    Extended = 1
}

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

public enum ResolutionType
{
    Repair = 0,
    Replace = 1,
    Refund = 2
}
