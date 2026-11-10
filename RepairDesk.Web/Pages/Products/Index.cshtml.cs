using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairDesk.Web.Models;
using RepairDesk.Web.Services;

namespace RepairDesk.Web.Pages.Products;

[Authorize(Roles = "Agent,Admin")]
public class IndexModel : PageModel
{
    private readonly RepairDeskApiClient _api;

    public IndexModel(RepairDeskApiClient api)
    {
        _api = api;
    }

    public IReadOnlyList<ProductDto> Products { get; set; } = Array.Empty<ProductDto>();
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Products = await _api.GetProductsAsync(cancellationToken);
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
