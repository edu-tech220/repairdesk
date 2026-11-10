using Microsoft.EntityFrameworkCore;
using RepairDesk.Api.Data;
using RepairDesk.Api.Exceptions;
using RepairDesk.Api.Models;
using RepairDesk.Api.Models.Enums;

namespace RepairDesk.Api.Services;

/// <summary>
/// Drives the RMA case state machine:
///
///   UnderReview --approve--> AwaitingReturn --receive--> Diagnosing --resolve-->
///     Resolved --close--> Closed
///   UnderReview --reject--> Rejected
///   Diagnosing --resolve(Replace, no stock)--> Backordered --fulfilled--> Resolved
///
/// Every transition is guarded by an If-Match RowVersion check against the value
/// the caller last saw, on top of EF Core's own concurrency token check at save time.
/// </summary>
public class RmaCaseService : IRmaCaseService
{
    private readonly RepairDeskDbContext _db;
    private readonly IInventoryService _inventory;

    public RmaCaseService(RepairDeskDbContext db, IInventoryService inventory)
    {
        _db = db;
        _inventory = inventory;
    }

    public async Task<RmaCase> SubmitClaimAsync(string serialNumber, string reportedIssue, CancellationToken cancellationToken = default)
    {
        var device = await _db.Devices
            .Include(d => d.Product)
            .FirstOrDefaultAsync(d => d.SerialNumber == serialNumber, cancellationToken);

        if (device is null)
        {
            throw new NotFoundException(nameof(RegisteredDevice), serialNumber);
        }

        var now = DateTime.UtcNow;
        var rmaCase = new RmaCase
        {
            DeviceId = device.Id,
            ReportedIssue = reportedIssue,
            CreatedAt = now,
            Status = CaseStatus.UnderReview
        };

        if (!WarrantyCalculator.IsUnderWarranty(device, now))
        {
            rmaCase.Status = CaseStatus.Rejected;
            rmaCase.DenialReason = $"Warranty expired on {device.WarrantyExpiresOn:yyyy-MM-dd}.";
        }

        _db.RmaCases.Add(rmaCase);
        AddEvent(rmaCase, CaseStatus.UnderReview, rmaCase.Status, "system", "Claim submitted.");

        await _db.SaveChangesAsync(cancellationToken);
        return rmaCase;
    }

    public async Task<RmaCase> ApproveAsync(int caseId, byte[] ifMatch, string actor, CancellationToken cancellationToken = default)
    {
        var rmaCase = await LoadCaseAsync(caseId, cancellationToken);
        EnsureRowVersionMatches(rmaCase, ifMatch);

        if (rmaCase.Status != CaseStatus.UnderReview)
        {
            throw new InvalidCaseTransitionException(rmaCase.Status, "approve");
        }

        var from = rmaCase.Status;
        rmaCase.Status = CaseStatus.AwaitingReturn;
        rmaCase.AwaitingReturnAt = DateTime.UtcNow;
        AddEvent(rmaCase, from, rmaCase.Status, actor, "Claim approved, awaiting the device.");

        await SaveWithConcurrencyCheckAsync(cancellationToken);
        return rmaCase;
    }

    public async Task<RmaCase> RejectAsync(int caseId, byte[] ifMatch, string actor, string reason, CancellationToken cancellationToken = default)
    {
        var rmaCase = await LoadCaseAsync(caseId, cancellationToken);
        EnsureRowVersionMatches(rmaCase, ifMatch);

        if (rmaCase.Status != CaseStatus.UnderReview)
        {
            throw new InvalidCaseTransitionException(rmaCase.Status, "reject");
        }

        var from = rmaCase.Status;
        rmaCase.Status = CaseStatus.Rejected;
        rmaCase.DenialReason = reason;
        AddEvent(rmaCase, from, rmaCase.Status, actor, reason);

        await SaveWithConcurrencyCheckAsync(cancellationToken);
        return rmaCase;
    }

    public async Task<RmaCase> ReceiveAsync(int caseId, byte[] ifMatch, string actor, string? trackingNumber, CancellationToken cancellationToken = default)
    {
        var rmaCase = await LoadCaseAsync(caseId, cancellationToken);
        EnsureRowVersionMatches(rmaCase, ifMatch);

        if (rmaCase.Status != CaseStatus.AwaitingReturn)
        {
            throw new InvalidCaseTransitionException(rmaCase.Status, "mark as received");
        }

        var from = rmaCase.Status;
        rmaCase.Status = CaseStatus.Diagnosing;
        rmaCase.ReceivedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(trackingNumber))
        {
            rmaCase.TrackingNumber = trackingNumber;
        }
        AddEvent(rmaCase, from, rmaCase.Status, actor, "Device received, diagnosis started.");

        await SaveWithConcurrencyCheckAsync(cancellationToken);
        return rmaCase;
    }

    public async Task<RmaCase> ResolveAsync(int caseId, byte[] ifMatch, string actor, ResolutionType resolution, string? notes, CancellationToken cancellationToken = default)
    {
        var rmaCase = await LoadCaseAsync(caseId, cancellationToken);
        EnsureRowVersionMatches(rmaCase, ifMatch);

        if (rmaCase.Status != CaseStatus.Diagnosing)
        {
            throw new InvalidCaseTransitionException(rmaCase.Status, "resolve");
        }

        var from = rmaCase.Status;
        rmaCase.Resolution = resolution;
        rmaCase.ResolutionNotes = notes;

        switch (resolution)
        {
            case ResolutionType.Repair:
                rmaCase.Status = CaseStatus.Resolved;
                rmaCase.ResolvedAt = DateTime.UtcNow;
                AddEvent(rmaCase, from, rmaCase.Status, actor, "Resolved by repair.");
                break;

            case ResolutionType.Refund:
                rmaCase.RefundAmount = RefundCalculator.CalculateRefund(rmaCase.Device!.Product!, rmaCase.Device!, DateTime.UtcNow);
                rmaCase.Status = CaseStatus.Resolved;
                rmaCase.ResolvedAt = DateTime.UtcNow;
                AddEvent(rmaCase, from, rmaCase.Status, actor, $"Resolved by refund of {rmaCase.RefundAmount:C}.");
                break;

            case ResolutionType.Replace:
                var reserved = await _inventory.TryReserveOneAsync(rmaCase.Device!.ProductId, cancellationToken);
                if (reserved)
                {
                    rmaCase.Status = CaseStatus.Resolved;
                    rmaCase.ResolvedAt = DateTime.UtcNow;
                    AddEvent(rmaCase, from, rmaCase.Status, actor, "Resolved by replacement unit.");
                }
                else
                {
                    rmaCase.Status = CaseStatus.Backordered;
                    AddEvent(rmaCase, from, rmaCase.Status, actor, "Replacement requested but no stock; backordered.");
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "Unknown resolution type.");
        }

        await SaveWithConcurrencyCheckAsync(cancellationToken);
        return rmaCase;
    }

    public async Task<RmaCase> CloseAsync(int caseId, byte[] ifMatch, string actor, CancellationToken cancellationToken = default)
    {
        var rmaCase = await LoadCaseAsync(caseId, cancellationToken);
        EnsureRowVersionMatches(rmaCase, ifMatch);

        if (rmaCase.Status != CaseStatus.Resolved)
        {
            throw new InvalidCaseTransitionException(rmaCase.Status, "close");
        }

        var from = rmaCase.Status;
        rmaCase.Status = CaseStatus.Closed;
        rmaCase.ClosedAt = DateTime.UtcNow;
        AddEvent(rmaCase, from, rmaCase.Status, actor, "Case closed.");

        await SaveWithConcurrencyCheckAsync(cancellationToken);
        return rmaCase;
    }

    public async Task<bool> TryFulfillBackorderAsync(int caseId, CancellationToken cancellationToken = default)
    {
        var rmaCase = await LoadCaseAsync(caseId, cancellationToken);

        if (rmaCase.Status != CaseStatus.Backordered)
        {
            return false;
        }

        var reserved = await _inventory.TryReserveOneAsync(rmaCase.Device!.ProductId, cancellationToken);
        if (!reserved)
        {
            return false;
        }

        var from = rmaCase.Status;
        rmaCase.Status = CaseStatus.Resolved;
        rmaCase.ResolvedAt = DateTime.UtcNow;
        AddEvent(rmaCase, from, rmaCase.Status, "system", "Backorder fulfilled from restocked inventory.");

        await SaveWithConcurrencyCheckAsync(cancellationToken);
        return true;
    }

    private async Task<RmaCase> LoadCaseAsync(int caseId, CancellationToken cancellationToken)
    {
        var rmaCase = await _db.RmaCases
            .Include(c => c.Device)
            .ThenInclude(d => d!.Product)
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);

        if (rmaCase is null)
        {
            throw new NotFoundException(nameof(RmaCase), caseId);
        }

        return rmaCase;
    }

    private static void EnsureRowVersionMatches(RmaCase rmaCase, byte[] ifMatch)
    {
        if (!rmaCase.RowVersion.SequenceEqual(ifMatch))
        {
            throw new ConcurrencyConflictException(
                $"Case {rmaCase.Id} was modified by someone else. Reload and try again.");
        }
    }

    private static void AddEvent(RmaCase rmaCase, CaseStatus from, CaseStatus to, string actor, string? note)
    {
        rmaCase.Events.Add(new CaseEvent
        {
            FromStatus = from,
            ToStatus = to,
            ActorUsername = actor,
            Note = note,
            OccurredAt = DateTime.UtcNow
        });
    }

    private async Task SaveWithConcurrencyCheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("The case was modified by someone else. Reload and try again.");
        }
    }
}
