using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairDesk.Web.Services;

namespace RepairDesk.Web.Pages.Products;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly RepairDeskApiClient _api;

    public CreateModel(RepairDeskApiClient api)
    {
        _api = api;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await _api.CreateProductAsync(
                Input.Sku, Input.Name, Input.Category, Input.Price,
                Input.StandardWarrantyMonths, Input.ExtendedWarrantyMonths, Input.InitialQuantity,
                cancellationToken);
            return RedirectToPage("/Products/Index");
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    public class InputModel
    {
        [Required]
        public string Sku { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(1, 120)]
        [Display(Name = "Standard warranty (months)")]
        public int StandardWarrantyMonths { get; set; } = 12;

        [Range(1, 120)]
        [Display(Name = "Extended warranty (months)")]
        public int ExtendedWarrantyMonths { get; set; } = 24;

        [Range(0, int.MaxValue)]
        [Display(Name = "Initial stock")]
        public int InitialQuantity { get; set; }
    }
}
