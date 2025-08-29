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

    public DbSet<User> Users => Set<User>();

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

        b.Entity<Approval>()
         .HasIndex(a => a.ExecutionId)
         .IsUnique()
         .HasDatabaseName("UX_Approval_Execution");

        b.Entity<User>().HasData(
            new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Executor 1",
                Role = UserRole.Executor
            },
            new User
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Executor 2",
                Role = UserRole.Executor
            },
            new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Supervisor",
                Role = UserRole.Supervisor
            }
        );

        base.OnModelCreating(b);
    }
}