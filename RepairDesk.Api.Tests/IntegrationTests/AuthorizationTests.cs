using System.Net;
using System.Net.Http.Json;
using RepairDesk.Api.Tests.Infrastructure;
using Xunit;

namespace RepairDesk.Api.Tests.IntegrationTests;

public class AuthorizationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public AuthorizationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        await TestDataSeeder.SeedUsersAsync(_factory);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("api/auth/login", new { username = TestDataSeeder.AdminUsername, password = TestDataSeeder.AdminPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await TestDataSeeder.SeedUsersAsync(_factory);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("api/auth/login", new { username = TestDataSeeder.AdminUsername, password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCases_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("api/rma-cases");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_AsAgent_ReturnsForbidden()
    {
        await TestDataSeeder.SeedUsersAsync(_factory);
        var client = await _factory.CreateAuthenticatedClientAsync(TestDataSeeder.AgentUsername, TestDataSeeder.AgentPassword);

        var response = await client.PostAsJsonAsync("api/products", new
        {
            sku = "FORBIDDEN-SKU",
            name = "Forbidden Product",
            category = "Test",
            price = 10m,
            standardWarrantyMonths = 12,
            extendedWarrantyMonths = 24,
            initialQuantity = 0
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_AsAdmin_Succeeds()
    {
        await TestDataSeeder.SeedUsersAsync(_factory);
        var client = await _factory.CreateAuthenticatedClientAsync(TestDataSeeder.AdminUsername, TestDataSeeder.AdminPassword);

        var response = await client.PostAsJsonAsync("api/products", new
        {
            sku = $"ALLOWED-{Guid.NewGuid():N}",
            name = "Allowed Product",
            category = "Test",
            price = 10m,
            standardWarrantyMonths = 12,
            extendedWarrantyMonths = 24,
            initialQuantity = 3
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
