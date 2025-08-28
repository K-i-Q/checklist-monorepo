namespace Checklist.Api.Domain;

public enum ExecutionStatus { Draft = 0, InProgress = 1, Submitted = 2, Approved = 3, Rejected = 4 }
public enum ItemStatus { Ok = 0, Nok = 1, NaoSeAplica = 2 }
public enum ApprovalDecision { Approve = 0, Reject = 1 }

public sealed class Vehicle
{
    public Guid Id { get; set; }
    public string Plate { get; set; } = string.Empty;
    public string? Model { get; set; }
}

public sealed class ChecklistTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<ChecklistTemplateItem> Items { get; set; } = new();
}

public sealed class ChecklistTemplateItem
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool Required { get; set; } = true;
}

public sealed class ChecklistExecution
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public Guid VehicleId { get; set; }

    public Guid? ExecutorId { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? LockedAt { get; set; }

    public ExecutionStatus Status { get; set; } = ExecutionStatus.Draft;
    public DateOnly? ReferenceDate { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public List<ChecklistExecutionItem> Items { get; set; } = new();
}

public sealed class ChecklistExecutionItem
{
    public Guid Id { get; set; }
    public Guid ExecutionId { get; set; }
    public Guid TemplateItemId { get; set; }
    public ItemStatus Status { get; set; }
    public string? Observation { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class Approval
{
    public Guid Id { get; set; }
    public Guid ExecutionId { get; set; }
    public Guid SupervisorId { get; set; }
    public ApprovalDecision Decision { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset DecidedAt { get; set; } = DateTimeOffset.UtcNow;
}