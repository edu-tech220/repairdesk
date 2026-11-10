using RepairDesk.Api.Models;
using RepairDesk.Api.Models.Enums;

namespace RepairDesk.Api.Services;

public interface IRmaCaseService
{
    Task<RmaCase> SubmitClaimAsync(string serialNumber, string reportedIssue, CancellationToken cancellationToken = default);
    Task<RmaCase> ApproveAsync(int caseId, byte[] ifMatch, string actor, CancellationToken cancellationToken = default);
    Task<RmaCase> RejectAsync(int caseId, byte[] ifMatch, string actor, string reason, CancellationToken cancellationToken = default);
    Task<RmaCase> ReceiveAsync(int caseId, byte[] ifMatch, string actor, string? trackingNumber, CancellationToken cancellationToken = default);
    Task<RmaCase> ResolveAsync(int caseId, byte[] ifMatch, string actor, ResolutionType resolution, string? notes, CancellationToken cancellationToken = default);
    Task<RmaCase> CloseAsync(int caseId, byte[] ifMatch, string actor, CancellationToken cancellationToken = default);
    Task<bool> TryFulfillBackorderAsync(int caseId, CancellationToken cancellationToken = default);
}
