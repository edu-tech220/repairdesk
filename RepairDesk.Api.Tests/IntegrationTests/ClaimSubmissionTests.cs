using System.Net;
using System.Net.Http.Json;
using RepairDesk.Api.Dtos;
using RepairDesk.Api.Models.Enums;
using RepairDesk.Api.Tests.Infrastructure;
using Xunit;

namespace RepairDesk.Api.Tests.IntegrationTests;

public class ClaimSubmissionTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ClaimSubmissionTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SubmitClaim_ForDeviceWithinWarranty_StartsUnderReview()
    {
        var serial = await TestDataSeeder.SeedDeviceAsync(_factory, "SN-IN-WARRANTY-1", purchasedMonthsAgo: 1);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("api/rma-cases", new { serialNumber = serial, reportedIssue = "Screen flickers intermittently." });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<RmaCaseDetailDto>(TestJson.Options);
        Assert.NotNull(created);
        Assert.Equal(CaseStatus.UnderReview, created!.Status);
        Assert.Null(created.DenialReason);
    }

    [Fact]
    public async Task SubmitClaim_ForExpiredWarranty_IsRejectedWithReason()
    {
        var serial = await TestDataSeeder.SeedDeviceAsync(_factory, "SN-EXPIRED-1", purchasedMonthsAgo: 36);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("api/rma-cases", new { serialNumber = serial, reportedIssue = "Won't power on." });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<RmaCaseDetailDto>(TestJson.Options);
        Assert.NotNull(created);
        Assert.Equal(CaseStatus.Rejected, created!.Status);
        Assert.NotNull(created.DenialReason);
    }

    [Fact]
    public async Task SubmitClaim_ForUnknownSerialNumber_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("api/rma-cases", new { serialNumber = "DOES-NOT-EXIST", reportedIssue = "Something is wrong." });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SubmitClaim_DoesNotRequireAuthentication()
    {
        var serial = await TestDataSeeder.SeedDeviceAsync(_factory, "SN-ANON-1");
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("api/rma-cases", new { serialNumber = serial, reportedIssue = "Anonymous submission check." });

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
