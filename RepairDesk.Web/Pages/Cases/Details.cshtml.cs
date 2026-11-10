using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairDesk.Web.Models;
using RepairDesk.Web.Services;

namespace RepairDesk.Web.Pages.Cases;

[Authorize(Roles = "Agent,Admin")]
public class DetailsModel : PageModel
{
    private readonly RepairDeskApiClient _api;

    public DetailsModel(RepairDeskApiClient api)
    {
        _api = api;
    }

    public RmaCaseDetailDto? Case { get; set; }
    public IReadOnlyList<CaseEventDto> History { get; set; } = Array.Empty<CaseEventDto>();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty]
    public string ETag { get; set; } = string.Empty;

    [BindProperty]
    public string? RejectReason { get; set; }

    [BindProperty]
    public string? TrackingNumber { get; set; }

    [BindProperty]
    public ResolutionType Resolution { get; set; }

    [BindProperty]
    public string? ResolutionNotes { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        await LoadAsync(id, cancellationToken);
        return Case is null ? NotFound() : Page();
    }

    public Task<IActionResult> OnPostApproveAsync(int id, CancellationToken cancellationToken) =>
        PerformActionAsync(id, () => _api.ApproveAsync(id, ETag, cancellationToken), "Claim approved.", cancellationToken);

    public Task<IActionResult> OnPostRejectAsync(int id, CancellationToken cancellationToken) =>
        PerformActionAsync(id, () => _api.RejectAsync(id, ETag, RejectReason ?? string.Empty, cancellationToken), "Claim rejected.", cancellationToken);

    public Task<IActionResult> OnPostReceiveAsync(int id, CancellationToken cancellationToken) =>
        PerformActionAsync(id, () => _api.ReceiveAsync(id, ETag, TrackingNumber, cancellationToken), "Device marked as received.", cancellationToken);

    public Task<IActionResult> OnPostResolveAsync(int id, CancellationToken cancellationToken) =>
        PerformActionAsync(id, () => _api.ResolveAsync(id, ETag, Resolution, ResolutionNotes, cancellationToken), "Case resolved.", cancellationToken);

    public Task<IActionResult> OnPostCloseAsync(int id, CancellationToken cancellationToken) =>
        PerformActionAsync(id, () => _api.CloseAsync(id, ETag, cancellationToken), "Case closed.", cancellationToken);

    private async Task<IActionResult> PerformActionAsync(int id, Func<Task<RmaCaseDetailDto>> action, string successMessage, CancellationToken cancellationToken)
    {
        try
        {
            await action();
            SuccessMessage = successMessage;
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }

        await LoadAsync(id, cancellationToken);
        return Page();
    }

    private async Task LoadAsync(int id, CancellationToken cancellationToken)
    {
        Case = await _api.GetCaseAsync(id, cancellationToken);
        if (Case is not null)
        {
            History = await _api.GetCaseHistoryAsync(id, cancellationToken);
            ETag = Case.ETag;
        }
    }
}
