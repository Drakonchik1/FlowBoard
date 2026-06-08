using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Events;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.ValueObjects;

namespace FlowBoard.UnitTests.Domain;

public sealed class WorkspaceTests
{
    private static Workspace CreateValidWorkspace(Guid? ownerId = null) =>
        Workspace.Create("Acme", WorkspaceSlug.Create("acme"), ownerId ?? Guid.NewGuid());

    [Fact]
    public void Create_ValidInputs_CreatorBecomesOwner()
    {
        var ownerId = Guid.NewGuid();
        var ws = Workspace.Create("Acme", WorkspaceSlug.Create("acme"), ownerId);

        Assert.Equal(ownerId, ws.OwnerId);
        Assert.Single(ws.Members);
        Assert.Equal(ownerId, ws.Members[0].UserId);
        Assert.Equal(WorkspaceMemberRole.Owner, ws.Members[0].Role);
        Assert.False(ws.IsDeleted);

        // WorkspaceCreated + MemberInvited (for owner) — both events
        Assert.Equal(2, ws.DomainEvents.Count);
        Assert.IsType<WorkspaceCreatedEvent>(ws.DomainEvents[0]);
        Assert.IsType<MemberInvitedEvent>(ws.DomainEvents[1]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_Throws(string name)
    {
        Assert.Throws<DomainException>(() =>
            Workspace.Create(name, WorkspaceSlug.Create("acme"), Guid.NewGuid()));
    }

    [Fact]
    public void Create_EmptyOwnerId_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Workspace.Create("Acme", WorkspaceSlug.Create("acme"), Guid.Empty));
    }

    [Fact]
    public void InviteMember_ValidUser_AddsAsMemberAndRaisesEvent()
    {
        var ws = CreateValidWorkspace();
        var inviteeId = Guid.NewGuid();

        var member = ws.InviteMember(inviteeId, WorkspaceMemberRole.Member);

        Assert.Equal(2, ws.Members.Count);
        Assert.Equal(WorkspaceMemberRole.Member, member.Role);
        Assert.Contains(ws.DomainEvents, e =>
            e is MemberInvitedEvent ev && ev.UserId == inviteeId);
    }

    [Fact]
    public void InviteMember_OwnerRole_Throws()
    {
        var ws = CreateValidWorkspace();
        Assert.Throws<DomainException>(() =>
            ws.InviteMember(Guid.NewGuid(), WorkspaceMemberRole.Owner));
    }

    [Fact]
    public void InviteMember_AlreadyMember_Throws()
    {
        var ws = CreateValidWorkspace();
        var userId = Guid.NewGuid();
        ws.InviteMember(userId, WorkspaceMemberRole.Member);

        Assert.Throws<DomainException>(() =>
            ws.InviteMember(userId, WorkspaceMemberRole.Admin));
    }

    [Fact]
    public void RemoveMember_Owner_Throws()
    {
        var ws = CreateValidWorkspace();
        Assert.Throws<DomainException>(() => ws.RemoveMember(ws.OwnerId));
    }

    [Fact]
    public void RemoveMember_NonExistent_Throws()
    {
        var ws = CreateValidWorkspace();
        Assert.Throws<NotFoundException>(() => ws.RemoveMember(Guid.NewGuid()));
    }

    [Fact]
    public void RemoveMember_Valid_RemovesAndRaisesEvent()
    {
        var ws = CreateValidWorkspace();
        var userId = Guid.NewGuid();
        ws.InviteMember(userId, WorkspaceMemberRole.Member);
        ws.ClearDomainEvents();

        ws.RemoveMember(userId);

        Assert.DoesNotContain(ws.Members, m => m.UserId == userId);
        Assert.Contains(ws.DomainEvents, e => e is MemberRemovedEvent ev && ev.UserId == userId);
    }

    [Fact]
    public void ChangeMemberRole_OwnerRole_Throws()
    {
        var ws = CreateValidWorkspace();
        var userId = Guid.NewGuid();
        ws.InviteMember(userId, WorkspaceMemberRole.Member);

        Assert.Throws<DomainException>(() =>
            ws.ChangeMemberRole(userId, WorkspaceMemberRole.Owner));
    }

    [Fact]
    public void ChangeMemberRole_OwnersRole_Throws()
    {
        var ws = CreateValidWorkspace();
        Assert.Throws<DomainException>(() =>
            ws.ChangeMemberRole(ws.OwnerId, WorkspaceMemberRole.Admin));
    }

    [Fact]
    public void ChangeMemberRole_Valid_ChangesAndRaisesEvent()
    {
        var ws = CreateValidWorkspace();
        var userId = Guid.NewGuid();
        ws.InviteMember(userId, WorkspaceMemberRole.Member);
        ws.ClearDomainEvents();

        ws.ChangeMemberRole(userId, WorkspaceMemberRole.Admin);

        Assert.Equal(WorkspaceMemberRole.Admin, ws.Members.First(m => m.UserId == userId).Role);
        Assert.Contains(ws.DomainEvents, e => e is MemberRoleChangedEvent);
    }

    // ── Authorization rules ──────────────────────────────────────────────

    [Fact]
    public void EnsureMember_NonMember_Throws()
    {
        var ws = CreateValidWorkspace();
        Assert.Throws<ForbiddenException>(() => ws.EnsureMember(Guid.NewGuid()));
    }

    [Fact]
    public void EnsureAdmin_RegularMember_Throws()
    {
        var ws = CreateValidWorkspace();
        var userId = Guid.NewGuid();
        ws.InviteMember(userId, WorkspaceMemberRole.Member);

        Assert.Throws<ForbiddenException>(() => ws.EnsureAdmin(userId));
    }

    [Fact]
    public void EnsureAdmin_AdminRole_Passes()
    {
        var ws = CreateValidWorkspace();
        var userId = Guid.NewGuid();
        ws.InviteMember(userId, WorkspaceMemberRole.Admin);

        ws.EnsureAdmin(userId); // should not throw
    }

    [Fact]
    public void EnsureAdmin_Owner_Passes()
    {
        var ws = CreateValidWorkspace();
        ws.EnsureAdmin(ws.OwnerId);
    }

    [Fact]
    public void EnsureOwner_Admin_Throws()
    {
        var ws = CreateValidWorkspace();
        var userId = Guid.NewGuid();
        ws.InviteMember(userId, WorkspaceMemberRole.Admin);

        Assert.Throws<ForbiddenException>(() => ws.EnsureOwner(userId));
    }

    [Fact]
    public void CanWrite_Viewer_IsFalse()
    {
        var ws = CreateValidWorkspace();
        var userId = Guid.NewGuid();
        ws.InviteMember(userId, WorkspaceMemberRole.Viewer);

        Assert.False(ws.CanWrite(userId));
        Assert.Throws<ForbiddenException>(() => ws.EnsureCanWrite(userId));
    }

    [Fact]
    public void CanWrite_Member_IsTrue()
    {
        var ws = CreateValidWorkspace();
        var userId = Guid.NewGuid();
        ws.InviteMember(userId, WorkspaceMemberRole.Member);

        Assert.True(ws.CanWrite(userId));
    }
}