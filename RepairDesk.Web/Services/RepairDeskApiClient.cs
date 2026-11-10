using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RepairDesk.Web.Models;

namespace RepairDesk.Web.Services;

public class RepairDeskApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;

    public RepairDeskApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<LoginResponse?> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new { username, password }, JsonOptions, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions, ct);
    }

    public async Task<RmaCaseDetailDto> SubmitClaimAsync(string serialNumber, string reportedIssue, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/rma-cases", new { serialNumber, reportedIssue }, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<RmaCaseDetailDto>(JsonOptions, ct))!;
    }

    public async Task<PagedResult<RmaCaseSummaryDto>> GetCasesAsync(CaseStatus? status, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (status is not null)
        {
            query.Add($"status={status}");
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add($"search={Uri.EscapeDataString(search)}");
        }

        var response = await _http.GetAsync($"api/rma-cases?{string.Join('&', query)}", ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<PagedResult<RmaCaseSummaryDto>>(JsonOptions, ct))!;
    }

    public async Task<RmaCaseDetailDto?> GetCaseAsync(int id, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/rma-cases/{id}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<RmaCaseDetailDto>(JsonOptions, ct);
    }

    public async Task<IReadOnlyList<CaseEventDto>> GetCaseHistoryAsync(int id, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/rma-cases/{id}/history", ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<List<CaseEventDto>>(JsonOptions, ct))!;
    }

    public Task<RmaCaseDetailDto> ApproveAsync(int id, string eTag, CancellationToken ct = default) =>
        PostActionAsync(id, "approve", eTag, null, ct);

    public Task<RmaCaseDetailDto> RejectAsync(int id, string eTag, string reason, CancellationToken ct = default) =>
        PostActionAsync(id, "reject", eTag, new { reason }, ct);

    public Task<RmaCaseDetailDto> ReceiveAsync(int id, string eTag, string? trackingNumber, CancellationToken ct = default) =>
        PostActionAsync(id, "receive", eTag, new { trackingNumber }, ct);

    public Task<RmaCaseDetailDto> ResolveAsync(int id, string eTag, ResolutionType resolution, string? notes, CancellationToken ct = default) =>
        PostActionAsync(id, "resolve", eTag, new { resolution, notes }, ct);

    public Task<RmaCaseDetailDto> CloseAsync(int id, string eTag, CancellationToken ct = default) =>
        PostActionAsync(id, "close", eTag, null, ct);

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/products", ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<List<ProductDto>>(JsonOptions, ct))!;
    }

    public async Task CreateProductAsync(string sku, string name, string category, decimal price, int standardMonths, int extendedMonths, int initialQuantity, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/products", new
        {
            sku,
            name,
            category,
            price,
            standardWarrantyMonths = standardMonths,
            extendedWarrantyMonths = extendedMonths,
            initialQuantity
        }, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task RestockAsync(int productId, int quantity, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"api/inventory/{productId}/restock", new { quantity }, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task<DeviceDto> RegisterDeviceAsync(string serialNumber, int productId, string ownerName, string ownerEmail, DateTime purchaseDate, WarrantyTier tier, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/devices", new
        {
            serialNumber,
            productId,
            ownerName,
            ownerEmail,
            purchaseDate,
            warrantyTier = tier
        }, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<DeviceDto>(JsonOptions, ct))!;
    }

    private async Task<RmaCaseDetailDto> PostActionAsync(int id, string action, string eTag, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/rma-cases/{id}/{action}");
        request.Headers.TryAddWithoutValidation("If-Match", eTag);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        var response = await _http.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<RmaCaseDetailDto>(JsonOptions, ct))!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string? detail = null;
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblem>(JsonOptions, ct);
            detail = problem?.Detail ?? problem?.Title;
        }
        catch (JsonException)
        {
            // Response body wasn't a ProblemDetails payload; fall back to the status code below.
        }

        throw new ApiException(detail ?? $"Request failed with status {(int)response.StatusCode}.", (int)response.StatusCode);
    }
}
