using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Features.Tags.Queries.GetTagsByWorkspace;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using Moq;

namespace FlowBoard.UnitTests.Handlers.Tags;

public sealed class GetTagsByWorkspaceQueryHandlerTests
{
    private readonly Mock<ITagRepository> _tagRepo = new();
    private readonly Mock<IWorkspaceRepository> _workspaceRepo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private GetTagsByWorkspaceQueryHandler CreateHandler() =>
        new(_tagRepo.Object, _workspaceRepo.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_NonMember_Throws404()
    {
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create("Acme", WorkspaceSlug.FromName("acme"), ownerId);

        _workspaceRepo.Setup(r => r.GetByIdWithMembersAsync(workspace.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workspace);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateHandler().Handle(new GetTagsByWorkspaceQuery(workspace.Id), CancellationToken.None));
    }
}
