using System.Net.Http.Headers;
using System.Net.Http.Json;
using RepairDesk.Api.Dtos;

namespace RepairDesk.Api.Tests.Infrastructure;

public static class TestAuthExtensions
{
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(this ApiWebApplicationFactory factory, string username, string password)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/auth/login", new { username, password });
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        return client;
    }
}
