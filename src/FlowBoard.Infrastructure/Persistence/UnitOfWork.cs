using FlowBoard.Application.Common.Events;
using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Domain.Common;
using FlowBoard.Domain.Interfaces;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlowBoard.Infrastructure.Persistence;

/// <summary>
/// Wraps DbContext.SaveChangesAsync and dispatches collected domain events after a successful commit.
/// Domain events are wrapped in DomainEventNotification so MediatR can route them to handlers.
/// </summary>
internal sealed class UnitOfWork(
    FlowBoardDbContext context,
    IPublisher publisher,
    ILogger<UnitOfWork> logger) : IUnitOfWork
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

        try
        {
            var result = await context.SaveChangesAsync(cancellationToken);

            foreach (var entity in entitiesWithEvents)
                entity.ClearDomainEvents();

            foreach (var domainEvent in domainEvents)
            {
                try
                {
                    var notification = DomainEventNotification.Wrap(domainEvent);
                    await publisher.Publish(notification, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Domain event {DomainEventType} notification failed after successful commit",
                        domainEvent.GetType().Name);
                }
            }

            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The resource was modified by another request. Please retry.");
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new ConflictException("A resource with this value already exists.");
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
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
