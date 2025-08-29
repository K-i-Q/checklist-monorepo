using Checklist.Api.Data;
using Checklist.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Checklist.Api.Services;

public static class DbSeeder
{
    /// <summary>
    /// Cria/migra o banco quando relacional e aplica um seed mínimo (idempotente):
    /// - Usuários (Executor 1/2 e Supervisor)
    /// - Veículo ABC1D23 (Sprinter)
    /// - Template "Saída padrão" com 3 itens
    /// </summary>
    public static async Task EnsureSeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChecklistDbContext>();

        if (db.Database.IsRelational())
        {
            logger.LogInformation("DB ▸ aplicando migrações…");
            await db.Database.MigrateAsync();
        }
        else
        {
            logger.LogInformation("DB ▸ EnsureCreated (InMemory) …");
            await db.Database.EnsureCreatedAsync();
        }

        logger.LogInformation("DB ▸ verificando seed mínimo…");

        var changed = false;

        var u1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var u2 = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var us = Guid.Parse("22222222-2222-2222-2222-222222222222");

        if (!await db.Users.AnyAsync(x => x.Id == u1))
        {
            db.Users.Add(new User { Id = u1, Name = "Executor 1", Role = UserRole.Executor });
            changed = true;
        }
        if (!await db.Users.AnyAsync(x => x.Id == u2))
        {
            db.Users.Add(new User { Id = u2, Name = "Executor 2", Role = UserRole.Executor });
            changed = true;
        }
        if (!await db.Users.AnyAsync(x => x.Id == us))
        {
            db.Users.Add(new User { Id = us, Name = "Supervisor", Role = UserRole.Supervisor });
            changed = true;
        }

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
            logger.LogInformation("DB ✓ seed aplicado (users/vehicle/template/itens).");
        }
        else
        {
            logger.LogInformation("DB ✓ nada a semear (já está ok).");
        }
    }
}