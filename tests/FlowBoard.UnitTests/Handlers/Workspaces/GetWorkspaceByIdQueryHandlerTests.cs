using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Workspaces.Queries.GetWorkspaceById;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Workspaces;

public sealed class GetWorkspaceByIdQueryHandlerTests
{
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private GetWorkspaceByIdQueryHandler CreateHandler() =>
        new(_workspaceRepo.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_Member_ReturnsWorkspaceDetail()
    {
        var userId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.Create("acme"), userId);
        _currentUser.Setup(c => c.UserId).Returns(userId);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        var result = await CreateHandler().Handle(new GetWorkspaceByIdQuery(workspace.Id), CancellationToken.None);

        Assert.Equal("Acme", result.Name);
        Assert.Equal(userId, result.OwnerId);
        Assert.Single(result.Members);
    }

    [Fact]
    public async Task Handle_NonMember_Returns404Not403()
    {
        var ownerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.Create("acme"), ownerId);
        _currentUser.Setup(c => c.UserId).Returns(outsiderId);
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new GetWorkspaceByIdQuery(workspace.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WorkspaceMissing_Returns404()
    {
        var workspaceId = Guid.NewGuid();
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workspace?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new GetWorkspaceByIdQuery(workspaceId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Anonymous_Throws401()
    {
        _currentUser.Setup(c => c.UserId).Returns((Guid?)null);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            CreateHandler().Handle(new GetWorkspaceByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
