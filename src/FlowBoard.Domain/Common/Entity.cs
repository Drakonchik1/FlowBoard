using FlowBoard.Domain.Events;

namespace FlowBoard.Domain.Common;

/// <summary>
/// Base class for all domain entities. Provides identity and a domain event collection
/// that is dispatched by UnitOfWork after SaveChangesAsync.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
