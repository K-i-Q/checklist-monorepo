using Checklist.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Checklist.Api.Data;

public sealed class ChecklistDbContext : DbContext
{
    public ChecklistDbContext(DbContextOptions<ChecklistDbContext> options)
        : base(options) { }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<ChecklistTemplate> Templates => Set<ChecklistTemplate>();
    public DbSet<ChecklistTemplateItem> TemplateItems => Set<ChecklistTemplateItem>();
    public DbSet<ChecklistExecution> Executions => Set<ChecklistExecution>();
    public DbSet<ChecklistExecutionItem> ExecutionItems => Set<ChecklistExecutionItem>();
    public DbSet<Approval> Approvals => Set<Approval>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Vehicle>()
            .HasIndex(v => v.Plate)
            .IsUnique();

        b.Entity<ChecklistTemplate>()
            .HasMany(t => t.Items)
            .WithOne()
            .HasForeignKey(i => i.TemplateId)
            .IsRequired();

        b.Entity<ChecklistTemplateItem>()
            .HasIndex(i => new { i.TemplateId, i.Order })
            .IsUnique();

        b.Entity<ChecklistExecution>()
            .Property(e => e.RowVersion)
            .IsRowVersion();

        b.Entity<ChecklistExecution>()
            .Property(e => e.ReferenceDate)
            .HasColumnType("date");

        b.Entity<ChecklistExecutionItem>()
            .HasIndex(i => new { i.ExecutionId, i.TemplateItemId })
            .IsUnique();

        b.Entity<ChecklistExecutionItem>()
            .Property(i => i.RowVersion)
            .IsRowVersion();

        b.Entity<ChecklistExecution>()
          .HasIndex(e => new { e.VehicleId, e.ReferenceDate })
          .HasFilter("[Status] IN (0,1)")
          .IsUnique()
          .HasDatabaseName("UX_ActiveExecution_Vehicle_Date");

        base.OnModelCreating(b);
    }
}