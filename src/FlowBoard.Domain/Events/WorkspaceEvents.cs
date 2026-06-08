using FlowBoard.Domain.Entities;

namespace FlowBoard.Domain.Events;

public sealed record WorkspaceCreatedEvent(Guid WorkspaceId, Guid OwnerId, string Name, string Slug) : IDomainEvent;

public sealed record MemberInvitedEvent(Guid WorkspaceId, Guid UserId, WorkspaceMemberRole Role) : IDomainEvent;

public sealed record MemberRemovedEvent(Guid WorkspaceId, Guid UserId) : IDomainEvent;

public sealed record MemberRoleChangedEvent(Guid WorkspaceId, Guid UserId, WorkspaceMemberRole NewRole) : IDomainEvent;