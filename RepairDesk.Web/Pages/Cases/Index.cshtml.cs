using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairDesk.Web.Models;
using RepairDesk.Web.Services;

namespace RepairDesk.Web.Pages.Cases;

[Authorize(Roles = "Agent,Admin")]
public class IndexModel : PageModel
{
    private const int PageSize = 10;

    private readonly RepairDeskApiClient _api;

    public IndexModel(RepairDeskApiClient api)
    {
        _api = api;
    }

    [BindProperty(SupportsGet = true)]
    public CaseStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<RmaCaseSummaryDto>? Cases { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Cases = await _api.GetCasesAsync(Status, Search, Math.Max(PageNumber, 1), PageSize, cancellationToken);
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
