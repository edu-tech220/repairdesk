using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairDesk.Web.Models;
using RepairDesk.Web.Services;

namespace RepairDesk.Web.Pages.Inventory;

[Authorize(Roles = "Admin")]
public class RestockModel : PageModel
{
    private readonly RepairDeskApiClient _api;

    public RestockModel(RepairDeskApiClient api)
    {
        _api = api;
    }

    [BindProperty(SupportsGet = true)]
    public int ProductId { get; set; }

    [BindProperty]
    [Range(1, int.MaxValue, ErrorMessage = "Enter a quantity of at least 1.")]
    public int Quantity { get; set; } = 1;

    public ProductDto? Product { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadProductAsync(cancellationToken);
        return Product is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadProductAsync(cancellationToken);
            return Page();
        }

        try
        {
            await _api.RestockAsync(ProductId, Quantity, cancellationToken);
            return RedirectToPage("/Products/Index");
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
            await LoadProductAsync(cancellationToken);
            return Page();
        }
    }

    private async Task LoadProductAsync(CancellationToken cancellationToken)
    {
        var products = await _api.GetProductsAsync(cancellationToken);
        Product = products.FirstOrDefault(p => p.Id == ProductId);
    }
}
