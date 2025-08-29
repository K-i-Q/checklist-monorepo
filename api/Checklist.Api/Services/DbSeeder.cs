using Checklist.Api.Data;
using Checklist.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Checklist.Api.Services;

public static class DbSeeder
{
    /// <summary>
    /// Aplica migrations e garante um seed mínimo (idempotente).
    /// - Veículo: ABC1D23 — Sprinter
    /// - Template: "Saída padrão" com 3 itens
    /// </summary>
    public static async Task EnsureSeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChecklistDbContext>();

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            logger.LogInformation("DB ▸ aplicando migrações…");
            await db.Database.MigrateAsync();

            logger.LogInformation("DB ▸ verificando seed mínimo…");

            var changed = false;

            var vehicleId = Guid.Parse("D83D3241-4710-4566-A118-662B80ECC543");
            var templateId = Guid.Parse("53948D28-DC6F-486A-9B04-19028A229BAB");

            if (!await db.Vehicles.AnyAsync(v => v.Id == vehicleId))
            {
                db.Vehicles.Add(new Vehicle
                {
                    Id = vehicleId,
                    Plate = "ABC1D23",
                    Model = "Sprinter"
                });
                changed = true;
            }

            if (!await db.Templates.AnyAsync(t => t.Id == templateId))
            {
                db.Templates.Add(new ChecklistTemplate
                {
                    Id = templateId,
                    Name = "Saída padrão"
                });
                changed = true;
            }

            var items = new[]
            {
                new ChecklistTemplateItem
                {
                    Id = Guid.Parse("C6DAEB8E-20E6-4B99-8CF6-ABCA1980ED5C"),
                    TemplateId = templateId,
                    Label = "Pneus calibrados",
                    Order = 1,
                    Required = true
                },
                new ChecklistTemplateItem
                {
                    Id = Guid.Parse("65ADB99F-2371-446D-A828-3883A7288057"),
                    TemplateId = templateId,
                    Label = "Faróis funcionando",
                    Order = 2,
                    Required = true
                },
                new ChecklistTemplateItem
                {
                    Id = Guid.Parse("EF64ABDA-B15E-4447-BB72-0AF044757103"),
                    TemplateId = templateId,
                    Label = "Kit de emergência",
                    Order = 3,
                    Required = false
                }
            };

            foreach (var it in items)
            {
                if (!await db.TemplateItems.AnyAsync(x => x.Id == it.Id))
                {
                    db.TemplateItems.Add(it);
                    changed = true;
                }
            }

            if (changed)
            {
                await db.SaveChangesAsync();
                logger.LogInformation("DB ✓ seed aplicado (vehicle/template/itens).");
            }
            else
            {
                logger.LogInformation("DB ✓ nada a semear (já está ok).");
            }
        });
    }
}