using System.Net;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Checklist.Api.Tests;

public class ApiSmokeTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public ApiSmokeTests(TestWebAppFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_endpoint_should_be_ok()
    {
        var res = await _client.GetAsync("/health");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Users_should_be_seeded()
    {
        var res = await _client.GetAsync("/api/checklists/users");
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync();
        var arr = JsonSerializer.Deserialize<List<UserDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        arr.Should().NotBeNull();
        arr!.Select(u => u.role).Should().BeEquivalentTo(new[] { "Executor", "Executor", "Supervisor" });
    }

    private record UserDto(Guid id, string name, string role);
}