using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Events;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.ValueObjects;

namespace FlowBoard.UnitTests.Domain;

public sealed class ProjectAndBoardTests
{
    [Fact]
    public void Project_Create_TrimsAndRaisesEvent()
    {
        var workspaceId = Guid.NewGuid();
        var project = Project.Create(workspaceId, "  Apollo  ", "  desc  ");

        Assert.Equal(workspaceId, project.WorkspaceId);
        Assert.Equal("Apollo", project.Name);
        Assert.Equal("desc", project.Description);
        Assert.Contains(project.DomainEvents, e => e is ProjectCreatedEvent);
    }

    [Fact]
    public void Project_Create_EmptyWorkspace_Throws()
    {
        Assert.Throws<DomainException>(() => Project.Create(Guid.Empty, "Apollo", null));
    }

    [Fact]
    public void Project_UpdateDescription_BlankBecomesNull()
    {
        var project = Project.Create(Guid.NewGuid(), "Apollo", "desc");
        project.UpdateDescription("   ");
        Assert.Null(project.Description);
    }

    [Fact]
    public void Board_Create_RequiresProjectAndWorkspace()
    {
        Assert.Throws<DomainException>(() => Board.Create(Guid.Empty, Guid.NewGuid(), "Board"));
        Assert.Throws<DomainException>(() => Board.Create(Guid.NewGuid(), Guid.Empty, "Board"));
    }

    [Fact]
    public void Board_Create_RaisesEvent()
    {
        var board = Board.Create(Guid.NewGuid(), Guid.NewGuid(), "Sprint Board");
        Assert.Equal("Sprint Board", board.Name);
        Assert.Contains(board.DomainEvents, e => e is BoardCreatedEvent);
    }

    [Fact]
    public void BoardList_MoveTo_UpdatesPosition()
    {
        var list = BoardList.Create(Guid.NewGuid(), "To Do", FractionalIndex.Start());
        var newPos = FractionalIndex.Between(list.Position, null);

        list.MoveTo(newPos);

        Assert.Equal(newPos.Value, list.Position.Value);
    }

    [Fact]
    public void BoardList_Create_BlankName_Throws()
    {
        Assert.Throws<DomainException>(() =>
            BoardList.Create(Guid.NewGuid(), " ", FractionalIndex.Start()));
    }
}
