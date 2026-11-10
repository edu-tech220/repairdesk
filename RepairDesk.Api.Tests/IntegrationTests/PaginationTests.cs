using System.Net.Http.Json;
using RepairDesk.Api.Dtos;
using RepairDesk.Api.Tests.Infrastructure;
using Xunit;

namespace RepairDesk.Api.Tests.IntegrationTests;

public class PaginationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public PaginationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_PagesResultsAccordingToPageSize()
    {
        await TestDataSeeder.SeedUsersAsync(_factory);
        var client = await _factory.CreateAuthenticatedClientAsync(TestDataSeeder.AgentUsername, TestDataSeeder.AgentPassword);

        for (var i = 0; i < 12; i++)
        {
            var serial = await TestDataSeeder.SeedDeviceAsync(_factory, $"SN-PAGE-{i}");
            var response = await client.PostAsJsonAsync("api/rma-cases", new { serialNumber = serial, reportedIssue = "Pagination test." });
            response.EnsureSuccessStatusCode();
        }

        var firstPage = await client.GetFromJsonAsync<PagedResult<RmaCaseSummaryDto>>("api/rma-cases?page=1&pageSize=5", TestJson.Options);
        var secondPage = await client.GetFromJsonAsync<PagedResult<RmaCaseSummaryDto>>("api/rma-cases?page=2&pageSize=5", TestJson.Options);
        var thirdPage = await client.GetFromJsonAsync<PagedResult<RmaCaseSummaryDto>>("api/rma-cases?page=3&pageSize=5", TestJson.Options);

        Assert.NotNull(firstPage);
        Assert.Equal(12, firstPage!.TotalCount);
        Assert.Equal(5, firstPage.Items.Count);
        Assert.Equal(5, secondPage!.Items.Count);
        Assert.Equal(2, thirdPage!.Items.Count);

        var allIds = firstPage.Items.Concat(secondPage.Items).Concat(thirdPage.Items).Select(c => c.Id).ToList();
        Assert.Equal(allIds.Distinct().Count(), allIds.Count);
    }

    [Fact]
    public async Task GetAll_FiltersBySerialNumberSearch()
    {
        await TestDataSeeder.SeedUsersAsync(_factory);
        var client = await _factory.CreateAuthenticatedClientAsync(TestDataSeeder.AgentUsername, TestDataSeeder.AgentPassword);

        var targetSerial = await TestDataSeeder.SeedDeviceAsync(_factory, "SN-FINDME-UNIQUE");
        await client.PostAsJsonAsync("api/rma-cases", new { serialNumber = targetSerial, reportedIssue = "Findable case." });

        var otherSerial = await TestDataSeeder.SeedDeviceAsync(_factory, "SN-OTHER-UNIQUE");
        await client.PostAsJsonAsync("api/rma-cases", new { serialNumber = otherSerial, reportedIssue = "Other case." });

        var result = await client.GetFromJsonAsync<PagedResult<RmaCaseSummaryDto>>("api/rma-cases?search=FINDME", TestJson.Options);

        Assert.NotNull(result);
        Assert.All(result!.Items, item => Assert.Contains("FINDME", item.SerialNumber));
    }
}
