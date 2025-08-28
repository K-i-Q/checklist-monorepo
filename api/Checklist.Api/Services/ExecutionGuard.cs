using Checklist.Api.Domain;

namespace Checklist.Api.Services;

public static class ExecutionGuard
{
    /// <summary>
    /// Garante que somente o executor que iniciou pode editar/enviar o checklist enquanto InProgress.
    /// </summary>
    public static void EnsureExecutor(Guid? currentUserId, ChecklistExecution exec)
    {
        if (exec.ExecutorId is null)
            throw new InvalidOperationException("Checklist ainda não foi iniciado. Use o endpoint /start.");

        if (exec.ExecutorId != currentUserId)
            throw new InvalidOperationException("Checklist está em execução por outro executor.");

        if (exec.Status != ExecutionStatus.InProgress)
            throw new InvalidOperationException($"Checklist não permite edição no status atual: {exec.Status}.");
    }
}