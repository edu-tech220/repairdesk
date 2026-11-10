using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RepairDesk.Api.Data;
using RepairDesk.Api.Dtos;
using RepairDesk.Api.Models.Enums;
using RepairDesk.Api.Services;
using RepairDesk.Api.Tests.Infrastructure;
using Xunit;

namespace RepairDesk.Api.Tests.IntegrationTests;

public class RmaCaseWorkflowTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public RmaCaseWorkflowTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HappyPath_RepairResolution_WalksThroughEveryStatus()
    {
        await TestDataSeeder.SeedUsersAsync(_factory);
        var serial = await TestDataSeeder.SeedDeviceAsync(_factory, "SN-REPAIR-1");
        var client = await _factory.CreateAuthenticatedClientAsync(TestDataSeeder.AgentUsername, TestDataSeeder.AgentPassword);

        var created = await SubmitAsync(client, serial);
        Assert.Equal(CaseStatus.UnderReview, created.Status);

        var approved = await PostActionAsync(client, created.Id, "approve", created.ETag);
        Assert.Equal(CaseStatus.AwaitingReturn, approved.Status);

        var received = await PostActionAsync(client, created.Id, "receive", approved.ETag, new { trackingNumber = "TRACK-123" });
        Assert.Equal(CaseStatus.Diagnosing, received.Status);

        var resolved = await PostActionAsync(client, created.Id, "resolve", received.ETag, new { resolution = "Repair", notes = "Replaced the flex cable." });
        Assert.Equal(CaseStatus.Resolved, resolved.Status);

        var closed = await PostActionAsync(client, created.Id, "close", resolved.ETag);
        Assert.Equal(CaseStatus.Closed, closed.Status);

        var history = await client.GetFromJsonAsync<List<CaseEventDto>>($"api/rma-cases/{created.Id}/history", TestJson.Options);
        Assert.NotNull(history);
        Assert.Equal(5, history!.Count);
    }

    [Fact]
    public async Task Reject_FromUnderReview_StopsTheCase()
    {
        await TestDataSeeder.SeedUsersAsync(_factory);
        var serial = await TestDataSeeder.SeedDeviceAsync(_factory, "SN-REJECT-1");
        var client = await _factory.CreateAuthenticatedClientAsync(TestDataSeeder.AgentUsername, TestDataSeeder.AgentPassword);

        var created = await SubmitAsync(client, serial);
        var rejected = await PostActionAsync(client, created.Id, "reject", created.ETag, new { reason = "Damage caused by water, not covered." });

        Assert.Equal(CaseStatus.Rejected, rejected.Status);
    }

    [Fact]
    public async Task Resolve_WithRefund_ComputesAProratedAmount()
    {
        await TestDataSeeder.SeedUsersAsync(_factory);
        var serial = await TestDataSeeder.SeedDeviceAsync(_factory, "SN-REFUND-1", price: 1000m, purchasedMonthsAgo: 1);
        var client = await _factory.CreateAuthenticatedClientAsync(TestDataSeeder.AgentUsername, TestDataSeeder.AgentPassword);

        var created = await SubmitAsync(client, serial);
        var approved = await PostActionAsync(client, created.Id, "approve", created.ETag);
        var received = await PostActionAsync(client, created.Id, "receive", approved.ETag, new { trackingNumber = (string?)null });
        var resolved = await PostActionAsync(client, created.Id, "resolve", received.ETag, new { resolution = "Refund", notes = (string?)null });

        Assert.Equal(CaseStatus.Resolved, resolved.Status);
        Assert.NotNull(resolved.RefundAmount);
        Assert.True(resolved.RefundAmount > 0m);
        Assert.True(resolved.RefundAmount <= 1000m);
    }

    [Fact]
    public async Task Resolve_WithReplaceAndStockAvailable_GoesStraightToResolved()
    {
        await TestDataSeeder.SeedUsersAsync(_factory);
        var serial = await TestDataSeeder.SeedDeviceAsync(_factory, "SN-REPLACE-STOCK-1", quantityOnHand: 2);
        var client = await _factory.CreateAuthenticatedClientAsync(TestDataSeeder.AgentUsername, TestDataSeeder.AgentPassword);

        var created = await SubmitAsync(client, serial);
        var approved = await PostActionAsync(client, created.Id, "approve", created.ETag);
        var received = await PostActionAsync(client, created.Id, "receive", approved.ETag, new { trackingNumber = (string?)null });
        var resolved = await PostActionAsync(client, created.Id, "resolve", received.ETag, new { resolution = "Replace", notes = (string?)null });

        Assert.Equal(CaseStatus.Resolved, resolved.Status);
    }

    [Fact]
    public async Task Resolve_WithReplaceAndNoStock_GoesBackorderedThenFulfillsAfterRestock()
    {
        await TestDataSeeder.SeedUsersAsync(_factory);
        var serial = await TestDataSeeder.SeedDeviceAsync(_factory, "SN-REPLACE-BACKORDER-1", quantityOnHand: 0);
        var client = await _factory.CreateAuthenticatedClientAsync(TestDataSeeder.AgentUsername, TestDataSeeder.AgentPassword);

        var created = await SubmitAsync(client, serial);
        var approved = await PostActionAsync(client, created.Id, "approve", created.ETag);
        var received = await PostActionAsync(client, created.Id, "receive", approved.ETag, new { trackingNumber = (string?)null });
        var resolved = await PostActionAsync(client, created.Id, "resolve", received.ETag, new { resolution = "Replace", notes = (string?)null });

        Assert.Equal(CaseStatus.Backordered, resolved.Status);

        // Simulate what the background worker does once stock arrives, without waiting on its timer.
        var inventory = await GetProductIdForDeviceAsync(serial);
        using (var scope = _factory.Services.CreateScope())
        {
            var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();
            await inventoryService.RestockAsync(inventory, 1);

            var caseService = scope.ServiceProvider.GetRequiredService<IRmaCaseService>();
            var fulfilled = await caseService.TryFulfillBackorderAsync(created.Id);
            Assert.True(fulfilled);
        }

        var afterFulfillment = await client.GetFromJsonAsync<RmaCaseDetailDto>($"api/rma-cases/{created.Id}", TestJson.Options);
        Assert.NotNull(afterFulfillment);
        Assert.Equal(CaseStatus.Resolved, afterFulfillment!.Status);
    }

    private async Task<int> GetProductIdForDeviceAsync(string serialNumber)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RepairDeskDbContext>();
        var device = await db.Devices.FirstAsync(d => d.SerialNumber == serialNumber);
        return device.ProductId;
    }

    private static async Task<RmaCaseDetailDto> SubmitAsync(HttpClient client, string serialNumber)
    {
        var response = await client.PostAsJsonAsync("api/rma-cases", new { serialNumber, reportedIssue = "Automated workflow test issue." });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RmaCaseDetailDto>(TestJson.Options))!;
    }

    private static async Task<RmaCaseDetailDto> PostActionAsync(HttpClient client, int caseId, string action, string eTag, object? body = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/rma-cases/{caseId}/{action}");
        request.Headers.TryAddWithoutValidation("If-Match", eTag);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: TestJson.Options);
        }

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RmaCaseDetailDto>(TestJson.Options))!;
    }
}
