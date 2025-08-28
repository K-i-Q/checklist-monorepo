using Checklist.Api.Domain;

namespace Checklist.Api.Dtos;

public sealed record CreateExecutionRequest(Guid TemplateId, Guid VehicleId, DateOnly? ReferenceDate);
public sealed record StartExecutionRequest(Guid ExecutorId);
public sealed record UpsertExecutionItemRequest(ItemStatus Status, string? Observation, byte[] RowVersion);
public sealed record SubmitExecutionRequest(byte[] RowVersion);
public sealed record ApproveRequest(ApprovalDecision Decision, string? Notes, byte[] RowVersion);