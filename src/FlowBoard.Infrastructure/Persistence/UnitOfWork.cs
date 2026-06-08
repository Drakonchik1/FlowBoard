using FlowBoard.Domain.Common;
using FlowBoard.Domain.Events;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Infrastructure.Persistence;

/// <summary>
/// Wraps DbContext.SaveChangesAsync and dispatches collected domain events after a successful commit.
/// Domain events are wrapped in DomainEventNotification so MediatR can route them to handlers.
/// </summary>
internal sealed class UnitOfWork(FlowBoardDbContext context, IPublisher publisher) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entitiesWithEvents = context.ChangeTracker
            .Entries<Entity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Clear events before saving — prevents duplicate dispatch if the operation is retried
        foreach (var entity in entitiesWithEvents)
            entity.ClearDomainEvents();

        var result = await context.SaveChangesAsync(cancellationToken);

        // Dispatch after successful commit only
        foreach (var domainEvent in domainEvents)
        {
            var notification = DomainEventNotification.Wrap(domainEvent);
            await publisher.Publish(notification, cancellationToken);
        }

        return result;
    }
}

/// <summary>
/// Wraps any IDomainEvent as a MediatR INotification.
/// Keeps Domain free of MediatR dependency while allowing Infrastructure to dispatch via IPublisher.
/// </summary>
internal sealed record DomainEventNotification(IDomainEvent DomainEvent) : INotification
{
    public static DomainEventNotification Wrap(IDomainEvent domainEvent) => new(domainEvent);
}
