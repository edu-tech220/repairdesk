using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairDesk.Web.Models;
using RepairDesk.Web.Services;

namespace RepairDesk.Web.Pages.Claims;

public class NewModel : PageModel
{
    private readonly RepairDeskApiClient _api;

    public NewModel(RepairDeskApiClient api)
    {
        _api = api;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public RmaCaseDetailDto? Result { get; set; }
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
            Result = await _api.SubmitClaimAsync(Input.SerialNumber, Input.ReportedIssue, cancellationToken);
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
        [Required(ErrorMessage = "Enter the device's serial number.")]
        [Display(Name = "Serial number")]
        public string SerialNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Describe the issue.")]
        [MinLength(5)]
        [Display(Name = "What's wrong?")]
        public string ReportedIssue { get; set; } = string.Empty;
    }
}
