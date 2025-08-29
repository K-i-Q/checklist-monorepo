using Checklist.Api.Data;
using Microsoft.EntityFrameworkCore;
using Checklist.Api.Endpoints;
using Checklist.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss ";
});
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.Hosting", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Error);

if (builder.Environment.IsEnvironment("Testing"))
{
    var testDbName = builder.Configuration["TestDbName"]
                     ?? $"ChecklistTests_{Guid.NewGuid():N}";

    builder.Services.AddDbContext<ChecklistDbContext>(opts =>
        opts.UseInMemoryDatabase(testDbName)
            .EnableDetailedErrors(false)
            .EnableSensitiveDataLogging(false));
}
else
{
    var cs =
        builder.Configuration.GetConnectionString("SqlServer")
        ?? builder.Configuration.GetConnectionString("Default")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer")
        ?? throw new InvalidOperationException("Connection string 'SqlServer' não configurada.");

    builder.Services.AddDbContext<ChecklistDbContext>(opts =>
        opts.UseSqlServer(cs, sql =>
        {
            sql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        })
        .EnableDetailedErrors(false)
        .EnableSensitiveDataLogging(false));
}

builder.Services.AddCors(o =>
    o.AddPolicy("ng", p => p
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
    )
);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks().AddDbContextCheck<ChecklistDbContext>();

var app = builder.Build();

await DbSeeder.EnsureSeedAsync(app.Services, app.Logger);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseCors("ng");
app.MapChecklistEndpoints();

app.Run();

public partial class Program { }