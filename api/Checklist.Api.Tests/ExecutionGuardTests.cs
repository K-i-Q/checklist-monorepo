using System;
using Checklist.Api.Domain;
using Checklist.Api.Services;
using FluentAssertions;
using Xunit;

namespace Checklist.Api.Tests;

public class ExecutionGuardTests
{
    [Fact]
    public void EnsureExecutor_should_allow_same_executor()
    {
        var same = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var exec = new ChecklistExecution
        {
            Id = Guid.NewGuid(),
            Status = ExecutionStatus.InProgress,
            ExecutorId = same
        };

        Action act = () => ExecutionGuard.EnsureExecutor(same, exec);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureExecutor_should_block_different_executor()
    {
        var owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var other = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var exec = new ChecklistExecution
        {
            Id = Guid.NewGuid(),
            Status = ExecutionStatus.InProgress,
            ExecutorId = owner
        };

        Action act = () => ExecutionGuard.EnsureExecutor(other, exec);

        act.Should()
           .Throw<InvalidOperationException>()
           .WithMessage("*por outro executor*");
    }
}