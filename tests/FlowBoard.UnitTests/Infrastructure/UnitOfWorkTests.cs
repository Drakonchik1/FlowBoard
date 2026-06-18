using FlowBoard.Application.Common.Events;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Events;
using FlowBoard.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowBoard.UnitTests.Infrastructure;

public sealed class UnitOfWorkTests
{
    private readonly Mock<IPublisher> _publisher = new();
    private static readonly ILogger<UnitOfWork> Logger = NullLogger<UnitOfWork>.Instance;

    [Fact]
    public async Task SaveChangesAsync_OnSuccess_ClearsDomainEventsAndPublishes()
    {
        await using var context = CreateContext();
        var user = User.Create("user@example.com", "Test User", "hash");
        context.Users.Add(user);

        var unitOfWork = new UnitOfWork(context, _publisher.Object, Logger);

        await unitOfWork.SaveChangesAsync();

        Assert.Empty(user.DomainEvents);
        _publisher.Verify(
            p => p.Publish(
                It.Is<DomainEventNotification>(n => n.DomainEvent is UserRegisteredEvent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveChangesAsync_OnFailure_RetainsDomainEventsAndDoesNotPublish()
    {
        await using var context = CreateContext(new ThrowingSaveChangesInterceptor());
        var user = User.Create("user@example.com", "Test User", "hash");
        context.Users.Add(user);

        var unitOfWork = new UnitOfWork(context, _publisher.Object, Logger);

        await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());

        Assert.Single(user.DomainEvents);
        Assert.IsType<UserRegisteredEvent>(user.DomainEvents[0]);
        _publisher.Verify(
            p => p.Publish(It.IsAny<DomainEventNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static FlowBoardDbContext CreateContext(params IInterceptor[] interceptors)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FlowBoardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString());

        if (interceptors.Length > 0)
            optionsBuilder.AddInterceptors(interceptors);

        return new FlowBoardDbContext(optionsBuilder.Options);
    }

    private sealed class ThrowingSaveChangesInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            throw new DbUpdateException("Simulated save failure.");
        }
    }
}
