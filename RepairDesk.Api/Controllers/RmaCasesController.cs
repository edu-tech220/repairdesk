using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RepairDesk.Api.Common;
using RepairDesk.Api.Data;
using RepairDesk.Api.Dtos;
using RepairDesk.Api.Exceptions;
using RepairDesk.Api.Models;
using RepairDesk.Api.Models.Enums;
using RepairDesk.Api.Services;

namespace RepairDesk.Api.Controllers;

[ApiController]
[Route("api/rma-cases")]
public class RmaCasesController : ControllerBase
{
    private readonly RepairDeskDbContext _db;
    private readonly IRmaCaseService _caseService;

    public RmaCasesController(RepairDeskDbContext db, IRmaCaseService caseService)
    {
        _db = db;
        _caseService = caseService;
    }

    /// <summary>Submit a new warranty claim by serial number. Anonymous and rate-limited.</summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("claims-submission")]
    public async Task<ActionResult<RmaCaseDetailDto>> Submit(SubmitClaimRequest request, CancellationToken cancellationToken)
    {
        var rmaCase = await _caseService.SubmitClaimAsync(request.SerialNumber, request.ReportedIssue, cancellationToken);
        await LoadNavigationsAsync(rmaCase, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = rmaCase.Id }, rmaCase.ToDetailDto());
    }

    /// <summary>List cases, paginated and optionally filtered by status or serial number.</summary>
    [HttpGet]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<ActionResult<PagedResult<RmaCaseSummaryDto>>> GetAll(
        [FromQuery] CaseStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.RmaCases
            .Include(c => c.Device)
            .ThenInclude(d => d!.Product)
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(c => c.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.Device!.SerialNumber.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<RmaCaseSummaryDto>(
            items.Select(c => c.ToSummaryDto()).ToList(),
            page,
            pageSize,
            totalCount);

        return Ok(result);
    }

    /// <summary>Get a single case's full detail. Sets an ETag header for use as a future If-Match.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<ActionResult<RmaCaseDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var rmaCase = await FindCaseAsync(id, cancellationToken);
        Response.Headers.ETag = ETag.ToHeaderValue(rmaCase.RowVersion);
        return Ok(rmaCase.ToDetailDto());
    }

    /// <summary>Get the ordered status-change history for a case.</summary>
    [HttpGet("{id:int}/history")]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<ActionResult<IReadOnlyList<CaseEventDto>>> GetHistory(int id, CancellationToken cancellationToken)
    {
        var events = await _db.CaseEvents
            .Where(e => e.CaseId == id)
            .OrderBy(e => e.OccurredAt)
            .ToListAsync(cancellationToken);

        if (events.Count == 0 && !await _db.RmaCases.AnyAsync(c => c.Id == id, cancellationToken))
        {
            throw new NotFoundException(nameof(RmaCase), id);
        }

        return Ok(events.Select(e => e.ToDto()));
    }

    /// <summary>Approve a claim under review. Requires an If-Match header with the case's current ETag.</summary>
    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<ActionResult<RmaCaseDetailDto>> Approve(int id, CancellationToken cancellationToken)
    {
        var ifMatch = RequireIfMatch();
        var rmaCase = await _caseService.ApproveAsync(id, ifMatch, User.Identity!.Name!, cancellationToken);
        return Ok(rmaCase.ToDetailDto());
    }

    /// <summary>Reject a claim under review with a reason. Requires an If-Match header.</summary>
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<ActionResult<RmaCaseDetailDto>> Reject(int id, RejectRequest request, CancellationToken cancellationToken)
    {
        var ifMatch = RequireIfMatch();
        var rmaCase = await _caseService.RejectAsync(id, ifMatch, User.Identity!.Name!, request.Reason, cancellationToken);
        return Ok(rmaCase.ToDetailDto());
    }

    /// <summary>Mark the returned device as received and move the case into diagnosis. Requires an If-Match header.</summary>
    [HttpPost("{id:int}/receive")]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<ActionResult<RmaCaseDetailDto>> Receive(int id, ReceiveRequest request, CancellationToken cancellationToken)
    {
        var ifMatch = RequireIfMatch();
        var rmaCase = await _caseService.ReceiveAsync(id, ifMatch, User.Identity!.Name!, request.TrackingNumber, cancellationToken);
        return Ok(rmaCase.ToDetailDto());
    }

    /// <summary>Resolve a case being diagnosed as Repair, Replace or Refund. Requires an If-Match header.</summary>
    [HttpPost("{id:int}/resolve")]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<ActionResult<RmaCaseDetailDto>> Resolve(int id, ResolveRequest request, CancellationToken cancellationToken)
    {
        var ifMatch = RequireIfMatch();
        var rmaCase = await _caseService.ResolveAsync(id, ifMatch, User.Identity!.Name!, request.Resolution, request.Notes, cancellationToken);
        return Ok(rmaCase.ToDetailDto());
    }

    /// <summary>Close a resolved case. Requires an If-Match header.</summary>
    [HttpPost("{id:int}/close")]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<ActionResult<RmaCaseDetailDto>> Close(int id, CancellationToken cancellationToken)
    {
        var ifMatch = RequireIfMatch();
        var rmaCase = await _caseService.CloseAsync(id, ifMatch, User.Identity!.Name!, cancellationToken);
        return Ok(rmaCase.ToDetailDto());
    }

    private byte[] RequireIfMatch()
    {
        if (!ETag.TryParse(Request.Headers.IfMatch, out var rowVersion))
        {
            throw new ConcurrencyConflictException("An If-Match header with the case's current ETag is required.");
        }

        return rowVersion;
    }

    private async Task<RmaCase> FindCaseAsync(int id, CancellationToken cancellationToken)
    {
        var rmaCase = await _db.RmaCases
            .Include(c => c.Device)
            .ThenInclude(d => d!.Product)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (rmaCase is null)
        {
            throw new NotFoundException(nameof(RmaCase), id);
        }

        return rmaCase;
    }

    private async Task LoadNavigationsAsync(RmaCase rmaCase, CancellationToken cancellationToken)
    {
        await _db.Entry(rmaCase).Reference(c => c.Device).LoadAsync(cancellationToken);
        if (rmaCase.Device is not null)
        {
            await _db.Entry(rmaCase.Device).Reference(d => d.Product).LoadAsync(cancellationToken);
        }
    }
}
