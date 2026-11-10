using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairDesk.Web.Models;
using RepairDesk.Web.Services;

namespace RepairDesk.Web.Pages.Devices;

[Authorize(Roles = "Agent,Admin")]
public class RegisterModel : PageModel
{
    private readonly RepairDeskApiClient _api;

    public RegisterModel(RepairDeskApiClient api)
    {
        _api = api;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<ProductDto> Products { get; set; } = Array.Empty<ProductDto>();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Products = await _api.GetProductsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Products = await _api.GetProductsAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var device = await _api.RegisterDeviceAsync(
                Input.SerialNumber, Input.ProductId, Input.OwnerName, Input.OwnerEmail,
                Input.PurchaseDate, Input.WarrantyTier, cancellationToken);
            SuccessMessage = $"Device {device.SerialNumber} registered. Warranty expires {device.WarrantyExpiresOn:yyyy-MM-dd}.";
            Input = new InputModel();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }

        return Page();
    }

    public class InputModel
    {
        [Required]
        [Display(Name = "Serial number")]
        public string SerialNumber { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Choose a product.")]
        [Display(Name = "Product")]
        public int ProductId { get; set; }

        [Required]
        [Display(Name = "Owner name")]
        public string OwnerName { get; set; } = string.Empty;

        [Required, EmailAddress]
        [Display(Name = "Owner email")]
        public string OwnerEmail { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Purchase date")]
        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow.Date;

        [Display(Name = "Warranty tier")]
        public WarrantyTier WarrantyTier { get; set; }
    }
}
