using Checklist.Api.Domain;

namespace Checklist.Api.Dtos;

public sealed record ExecutionItemResponse(
    Guid Id,
    Guid ExecutionId,
    Guid TemplateItemId,
    ItemStatus Status,
    string? Observation,
    byte[] RowVersion
);

public sealed record ExecutionResponse(
    Guid Id,
    Guid TemplateId,
    Guid VehicleId,
    Guid? ExecutorId,
    DateTimeOffset? StartedAt,
    DateTimeOffset? LockedAt,
    ExecutionStatus Status,
    DateOnly? ReferenceDate,
    byte[] RowVersion,
    IReadOnlyList<ExecutionItemResponse> Items
);