using FlowBoard.Domain.Common;
using FlowBoard.Domain.Events;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.ValueObjects;

namespace FlowBoard.Domain.Entities;

/// <summary>
/// Aggregate root for a workspace. Contains its members and enforces all membership invariants.
/// All mutations go through factory or named methods — no public setters.
/// Authorization rules (EnsureMember, EnsureAdmin, EnsureOwner) live here so handlers
/// stay thin and the rules are uniformly enforced regardless of caller.
/// </summary>
public sealed class Workspace : Entity
{
    private const int NameMinLength = 1;
    private const int NameMaxLength = 100;

    public string Name { get; private set; } = null!;
    public WorkspaceSlug Slug { get; private set; } = null!;
    public Guid OwnerId { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<WorkspaceMember> _members = [];
    public IReadOnlyList<WorkspaceMember> Members => _members.AsReadOnly();

    // Required by EF Core
    private Workspace() { }

    /// <summary>
    /// Creates a workspace and assigns the creator as the Owner.
    /// Raises WorkspaceCreatedEvent and MemberInvitedEvent for the owner.
    /// </summary>
    public static Workspace Create(string name, WorkspaceSlug slug, Guid ownerId)
    {
        ValidateName(name);
        if (ownerId == Guid.Empty)
            throw new DomainException("Workspace must have an owner.");

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Slug = slug,
            OwnerId = ownerId,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var ownerMember = WorkspaceMember.Create(workspace.Id, ownerId, WorkspaceMemberRole.Owner);
        workspace._members.Add(ownerMember);

        workspace.Raise(new WorkspaceCreatedEvent(workspace.Id, ownerId, workspace.Name, slug.Value));
        workspace.Raise(new MemberInvitedEvent(workspace.Id, ownerId, WorkspaceMemberRole.Owner));

        return workspace;
    }

    /// <summary>Updates the display name. Owner/Admin only — caller enforces.</summary>
    public void Rename(string newName)
    {
        ValidateName(newName);
        Name = newName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Soft-deletes. The matching query filter on RefreshToken+Members hides them automatically.</summary>
    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Membership management ────────────────────────────────────────────────

    /// <summary>
    /// Invites a new user with the given role. Owner role can never be assigned via invite —
    /// only the workspace creator is Owner, and ownership transfer is a separate operation.
    /// </summary>
    public WorkspaceMember InviteMember(Guid userId, WorkspaceMemberRole role)
    {
        if (userId == Guid.Empty)
            throw new DomainException("UserId is required.");

        if (role == WorkspaceMemberRole.Owner)
            throw new DomainException("Owner role cannot be assigned via invite. Use ownership transfer.");

        if (_members.Any(m => m.UserId == userId))
            throw new DomainException("User is already a member of this workspace.");

        var member = WorkspaceMember.Create(Id, userId, role);
        _members.Add(member);
        UpdatedAt = DateTime.UtcNow;

        Raise(new MemberInvitedEvent(Id, userId, role));
        return member;
    }

    /// <summary>Removes a member. The workspace Owner cannot be removed.</summary>
    public void RemoveMember(Guid userId)
    {
        if (userId == OwnerId)
            throw new DomainException("The workspace Owner cannot be removed. Transfer ownership first.");

        var member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new NotFoundException("WorkspaceMember", userId);

        _members.Remove(member);
        UpdatedAt = DateTime.UtcNow;
        Raise(new MemberRemovedEvent(Id, userId));
    }

    /// <summary>Changes a member's role. The Owner's role cannot be changed (use ownership transfer).</summary>
    public void ChangeMemberRole(Guid userId, WorkspaceMemberRole newRole)
    {
        if (userId == OwnerId)
            throw new DomainException("The Owner's role cannot be changed. Use ownership transfer.");

        if (newRole == WorkspaceMemberRole.Owner)
            throw new DomainException("Owner role cannot be assigned this way. Use ownership transfer.");

        var member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new NotFoundException("WorkspaceMember", userId);

        if (member.Role == newRole)
            return; // no-op

        member.ChangeRole(newRole);
        UpdatedAt = DateTime.UtcNow;
        Raise(new MemberRoleChangedEvent(Id, userId, newRole));
    }

    // ── Authorization rules (the source of truth) ────────────────────────────

    public WorkspaceMemberRole? GetRole(Guid userId) =>
        _members.FirstOrDefault(m => m.UserId == userId)?.Role;

    public bool HasMember(Guid userId) => _members.Any(m => m.UserId == userId);

    public bool IsAdmin(Guid userId) =>
        GetRole(userId) is WorkspaceMemberRole.Owner or WorkspaceMemberRole.Admin;

    public bool IsOwner(Guid userId) => OwnerId == userId;

    public bool CanWrite(Guid userId)
    {
        var role = GetRole(userId);
        return role is WorkspaceMemberRole.Owner or WorkspaceMemberRole.Admin or WorkspaceMemberRole.Member;
    }

    /// <summary>Throws ForbiddenException unless the user is a member of this workspace.</summary>
    public void EnsureMember(Guid userId)
    {
        if (!HasMember(userId))
            throw new ForbiddenException("You are not a member of this workspace.");
    }

    /// <summary>Throws ForbiddenException unless the user is Owner or Admin.</summary>
    public void EnsureAdmin(Guid userId)
    {
        if (!IsAdmin(userId))
            throw new ForbiddenException("This action requires Admin or Owner role.");
    }

    /// <summary>Throws ForbiddenException unless the user is the Owner.</summary>
    public void EnsureOwner(Guid userId)
    {
        if (!IsOwner(userId))
            throw new ForbiddenException("This action requires Owner role.");
    }

    /// <summary>Throws ForbiddenException unless the user has write access (Owner/Admin/Member).</summary>
    public void EnsureCanWrite(Guid userId)
    {
        if (!CanWrite(userId))
            throw new ForbiddenException("This action requires write access.");
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Workspace name cannot be empty.");

        if (name.Length is < NameMinLength or > NameMaxLength)
            throw new DomainException($"Workspace name must be between {NameMinLength} and {NameMaxLength} characters.");
    }
}