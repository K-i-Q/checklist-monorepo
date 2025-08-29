using System.Net;
using System.Net.Http.Json;
using Checklist.Api.Data;
using Checklist.Api.Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Checklist.Api.Tests;

public class StartLockApiTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public StartLockApiTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Start_should_lock_execution_for_first_executor_and_block_second()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ChecklistDbContext>();
            var exec = new ChecklistExecution
            {
                Id = Guid.NewGuid(),
                TemplateId = Guid.NewGuid(),
                VehicleId = Guid.NewGuid(),
                Status = ExecutionStatus.Draft
            };
            db.Executions.Add(exec);
            await db.SaveChangesAsync();

            var resA = await _client.PostAsJsonAsync($"/api/checklists/executions/{exec.Id}/start",
                new { executorId = Guid.Parse("11111111-1111-1111-1111-111111111111") });
            resA.StatusCode.Should().Be(HttpStatusCode.OK);

            var resB = await _client.PostAsJsonAsync($"/api/checklists/executions/{exec.Id}/start",
                new { executorId = Guid.Parse("33333333-3333-3333-3333-333333333333") });
            resB.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
    }
}