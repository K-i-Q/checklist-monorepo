using Checklist.Api.Data;
using Checklist.Api.Domain;
using Checklist.Api.Dtos;
using Checklist.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace Checklist.Api.Endpoints;

public static class ChecklistEndpoints
{
    public static RouteGroupBuilder MapChecklistEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/checklists");

        (Guid? id, string role) GetUser(HttpRequest r)
        {
            Guid? id = r.Headers.TryGetValue("X-User-Id", out var v) && Guid.TryParse(v, out var g) ? g : null;
            var role = r.Headers.TryGetValue("X-User-Role", out var rr) ? rr.ToString() : "Executor";
            return (id, role);
        }

        grp.MapGet("/templates/{templateId:guid}/items", async (Guid templateId, ChecklistDbContext db) =>
            await db.TemplateItems
                .Where(i => i.TemplateId == templateId)
                .OrderBy(i => i.Order)
                .Select(i => new { id = i.Id, label = i.Label, order = i.Order, required = i.Required })
                .ToListAsync());

        grp.MapGet("/vehicles", async (ChecklistDbContext db) =>
            await db.Vehicles
                .Select(v => new { v.Id, v.Plate, v.Model })
                .OrderBy(v => v.Plate)
                .ToListAsync());

        grp.MapGet("/templates", async (ChecklistDbContext db) =>
            await db.Templates
                .Select(t => new { t.Id, t.Name })
                .OrderBy(t => t.Name)
                .ToListAsync());


        grp.MapPost("/executions", async (CreateExecutionRequest req, ChecklistDbContext db, HttpResponse res) =>
        {
            var tpl = await db.Templates
                            .Include(t => t.Items)
                            .FirstOrDefaultAsync(t => t.Id == req.TemplateId);
            if (tpl is null)
                return Results.NotFound("Template não encontrado.");

            var existsVehicle = await db.Vehicles.AnyAsync(v => v.Id == req.VehicleId);
            if (!existsVehicle)
                return Results.NotFound("Veículo não encontrado.");

            var rd = req.ReferenceDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);

            var existing = await db.Executions
                .Where(e => e.VehicleId == req.VehicleId
                            && e.ReferenceDate == rd
                            && (e.Status == ExecutionStatus.Draft || e.Status == ExecutionStatus.InProgress))
                .Select(e => new { e.Id })
                .FirstOrDefaultAsync();

            if (existing is not null)
            {
                res.Headers.Location = $"/api/checklists/executions/{existing.Id}";
                res.Headers.Append("X-Existing-Execution-Id", existing.Id.ToString());

                return Results.Conflict(new
                {
                    message = "Já existe uma execução ativa para este veículo nesta data.",
                    existing = new { id = existing.Id }
                });
            }

            var exec = new ChecklistExecution
            {
                Id = Guid.NewGuid(),
                TemplateId = req.TemplateId,
                VehicleId = req.VehicleId,
                Status = ExecutionStatus.Draft,
                ReferenceDate = rd
            };

            exec.Items = tpl.Items
                .OrderBy(i => i.Order)
                .Select(i => new ChecklistExecutionItem
                {
                    Id = Guid.NewGuid(),
                    ExecutionId = exec.Id,
                    TemplateItemId = i.Id,
                    Status = ItemStatus.NaoSeAplica
                })
                .ToList();

            db.Executions.Add(exec);
            await db.SaveChangesAsync();

            return Results.Created($"/api/checklists/executions/{exec.Id}", new { exec.Id });
        });


        grp.MapPost("/executions/{id:guid}/start", async (Guid id, StartExecutionRequest req, ChecklistDbContext db) =>
        {
            var exec = await db.Executions.FindAsync(id);
            if (exec is null) return Results.NotFound();

            if (exec.ExecutorId is not null && exec.ExecutorId != req.ExecutorId)
                return Results.Conflict("Já iniciado por outro executor.");

            exec.ExecutorId ??= req.ExecutorId;
            exec.StartedAt ??= DateTimeOffset.UtcNow;
            exec.LockedAt = DateTimeOffset.UtcNow;
            exec.Status = ExecutionStatus.InProgress;

            await db.SaveChangesAsync();
            return Results.Ok();
        });

        grp.MapPatch("/executions/{id:guid}/items/{templateItemId:guid}",
        async (Guid id, Guid templateItemId, UpsertExecutionItemRequest req, ChecklistDbContext db, HttpRequest http) =>
        {
            var (userId, _) = GetUser(http);

            var exec = await db.Executions
                            .Include(e => e.Items)
                            .FirstOrDefaultAsync(e => e.Id == id);
            if (exec is null) return Results.NotFound();

            try { ExecutionGuard.EnsureExecutor(userId, exec); }
            catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 403); }

            if (exec.Status != ExecutionStatus.InProgress)
                return Results.BadRequest("Itens só podem ser editados quando a execução estiver InProgress.");

            var item = exec.Items.FirstOrDefault(i => i.TemplateItemId == templateItemId);
            if (item is null) return Results.NotFound("Item não pertence ao checklist.");

            db.Entry(item).Property(i => i.RowVersion).OriginalValue = req.RowVersion;

            item.Status = req.Status;
            item.Observation = string.IsNullOrWhiteSpace(req.Observation) ? null : req.Observation.Trim();

            try
            {
                await db.SaveChangesAsync();
                return Results.NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict("Versão desatualizada (item). Os dados foram recarregados, tente novamente.");
            }
        });

        grp.MapPost("/executions/{id:guid}/submit",
        async (Guid id, SubmitExecutionRequest req, ChecklistDbContext db, HttpRequest http) =>
        {
            var (userId, _) = GetUser(http);

            var exec = await db.Executions.FirstOrDefaultAsync(e => e.Id == id);
            if (exec is null) return Results.NotFound();

            try { ExecutionGuard.EnsureExecutor(userId, exec); }
            catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 403); }

            db.Entry(exec).Property(e => e.RowVersion).OriginalValue = req.RowVersion;

            var requiredTemplateItems = await db.TemplateItems
                .Where(ti => ti.TemplateId == exec.TemplateId && ti.Required)
                .Select(ti => ti.Id)
                .ToListAsync();

            var requiredStatuses = await db.ExecutionItems
                .Where(ei => ei.ExecutionId == exec.Id && requiredTemplateItems.Contains(ei.TemplateItemId))
                .Select(ei => ei.Status)
                .ToListAsync();

            if (requiredStatuses.Any(s => s == ItemStatus.NaoSeAplica))
                return Results.BadRequest("Existem itens obrigatórios marcados como N/A. Preencha antes de enviar.");

            exec.Status = ExecutionStatus.Submitted;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict("Versão desatualizada. Recarregue os dados.");
            }

            return Results.Ok();
        });

        grp.MapGet("/users", async (ChecklistDbContext db) =>
        await db.Users
            .OrderBy(u => u.Role)
            .ThenBy(u => u.Name)
            .Select(u => new { u.Id, u.Name, role = u.Role.ToString() })
            .ToListAsync());

        grp.MapGet("/executions/active", async (Guid vehicleId, DateOnly date, ChecklistDbContext db) =>
        {
            var active = await db.Executions
                .Where(e => e.VehicleId == vehicleId
                        && e.ReferenceDate == date
                        && (e.Status == ExecutionStatus.Draft || e.Status == ExecutionStatus.InProgress))
                .Select(e => new { e.Id })
                .FirstOrDefaultAsync();

            return active is null ? Results.NotFound() : Results.Ok(active);
        });

        grp.MapPost("/executions/{id:guid}/approve",
        async (Guid id, ApproveRequest req, ChecklistDbContext db, HttpRequest http) =>
        {
            var (userId, role) = GetUser(http);
            if (!string.Equals(role, "Supervisor", StringComparison.OrdinalIgnoreCase))
                return Results.Problem(
                    "Somente Supervisor pode aprovar/reprovar.",
                    statusCode: 403
                );

            var exec = await db.Executions.FirstOrDefaultAsync(e => e.Id == id);
            if (exec is null) return Results.NotFound();

            db.Entry(exec).Property(e => e.RowVersion).OriginalValue = req.RowVersion;

            exec.Status = req.Decision == ApprovalDecision.Approve
                ? ExecutionStatus.Approved
                : ExecutionStatus.Rejected;

            db.Approvals.Add(new Approval
            {
                Id = Guid.NewGuid(),
                ExecutionId = id,
                SupervisorId = userId ?? Guid.Empty,
                Decision = req.Decision,
                Notes = req.Notes
            });

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Conflict("Versão desatualizada. Recarregue os dados.");
            }

            return Results.Ok();
        });

        grp.MapGet("/executions/{id:guid}", async (Guid id, ChecklistDbContext db) =>
        {
            var exec = await db.Executions
                .Include(e => e.Items)
                .FirstOrDefaultAsync(e => e.Id == id);

            return exec is null ? Results.NotFound() : Results.Ok(exec);
        });

        return grp;
    }
}