using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Domain.Common;
using FlowBoard.Domain.Events;
using FlowBoard.Domain.Interfaces;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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

        foreach (var entity in entitiesWithEvents)
            entity.ClearDomainEvents();

        try
        {
            var result = await context.SaveChangesAsync(cancellationToken);

            foreach (var domainEvent in domainEvents)
            {
                var notification = DomainEventNotification.Wrap(domainEvent);
                await publisher.Publish(notification, cancellationToken);
            }

            return result;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new ConflictException("A resource with this value already exists.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        for (var inner = ex.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (inner is SqlException { Number: 2627 or 2601 })
                return true;
        }

        return false;
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
