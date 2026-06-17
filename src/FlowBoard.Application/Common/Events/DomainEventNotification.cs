using FlowBoard.Domain.Events;
using MediatR;

namespace FlowBoard.Application.Common.Events;

/// <summary>
/// Wraps any <see cref="IDomainEvent"/> as a MediatR <see cref="INotification"/>.
/// Dispatched by <see cref="FlowBoard.Infrastructure.Persistence.UnitOfWork"/> after a successful commit.
/// </summary>
public sealed record DomainEventNotification(IDomainEvent DomainEvent) : INotification
{
    public static DomainEventNotification Wrap(IDomainEvent domainEvent) => new(domainEvent);
}
