using System.Net;
using System.Net.Http.Json;
using RepairDesk.Api.Dtos;
using RepairDesk.Api.Tests.Infrastructure;
using Xunit;

namespace RepairDesk.Api.Tests.IntegrationTests;

public class ConcurrencyTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ConcurrencyTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Approve_WithAnETagSomeoneElseAlreadyConsumed_ReturnsPreconditionFailed()
    {
        await TestDataSeeder.SeedUsersAsync(_factory);
        var serial = await TestDataSeeder.SeedDeviceAsync(_factory, "SN-CONCURRENCY-1");
        var client = await _factory.CreateAuthenticatedClientAsync(TestDataSeeder.AgentUsername, TestDataSeeder.AgentPassword);

        var submitResponse = await client.PostAsJsonAsync("api/rma-cases", new { serialNumber = serial, reportedIssue = "Concurrency test." });
        var created = (await submitResponse.Content.ReadFromJsonAsync<RmaCaseDetailDto>(TestJson.Options))!;

        // First approval succeeds and rotates the RowVersion.
        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, $"api/rma-cases/{created.Id}/approve");
        firstRequest.Headers.TryAddWithoutValidation("If-Match", created.ETag);
        var firstResponse = await client.SendAsync(firstRequest);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Retrying the same stale ETag (as a second caller racing the first would) must fail.
        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, $"api/rma-cases/{created.Id}/approve");
        secondRequest.Headers.TryAddWithoutValidation("If-Match", created.ETag);
        var secondResponse = await client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.PreconditionFailed, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Approve_WithoutIfMatchHeader_ReturnsPreconditionFailed()
    {
        await TestDataSeeder.SeedUsersAsync(_factory);
        var serial = await TestDataSeeder.SeedDeviceAsync(_factory, "SN-CONCURRENCY-2");
        var client = await _factory.CreateAuthenticatedClientAsync(TestDataSeeder.AgentUsername, TestDataSeeder.AgentPassword);

        var submitResponse = await client.PostAsJsonAsync("api/rma-cases", new { serialNumber = serial, reportedIssue = "Concurrency test." });
        var created = (await submitResponse.Content.ReadFromJsonAsync<RmaCaseDetailDto>(TestJson.Options))!;

        var response = await client.PostAsync($"api/rma-cases/{created.Id}/approve", content: null);

        Assert.Equal(HttpStatusCode.PreconditionFailed, response.StatusCode);
    }

    [Fact]
    public async Task Approve_ThenResolveFromWrongStatus_ReturnsConflict()
    {
        await TestDataSeeder.SeedUsersAsync(_factory);
        var serial = await TestDataSeeder.SeedDeviceAsync(_factory, "SN-CONCURRENCY-3");
        var client = await _factory.CreateAuthenticatedClientAsync(TestDataSeeder.AgentUsername, TestDataSeeder.AgentPassword);

        var submitResponse = await client.PostAsJsonAsync("api/rma-cases", new { serialNumber = serial, reportedIssue = "Wrong status transition test." });
        var created = (await submitResponse.Content.ReadFromJsonAsync<RmaCaseDetailDto>(TestJson.Options))!;

        // Case is still UnderReview, so trying to resolve it (which requires Diagnosing) must fail.
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/rma-cases/{created.Id}/resolve");
        request.Headers.TryAddWithoutValidation("If-Match", created.ETag);
        request.Content = JsonContent.Create(new { resolution = "Repair", notes = (string?)null }, options: TestJson.Options);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
