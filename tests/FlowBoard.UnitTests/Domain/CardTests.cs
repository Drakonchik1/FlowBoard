using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Events;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.ValueObjects;

namespace FlowBoard.UnitTests.Domain;

public sealed class CardTests
{
    private static FractionalIndex Pos() => FractionalIndex.Start();

    [Fact]
    public void Create_SetsFieldsAndRaisesEvent()
    {
        var boardId = Guid.NewGuid();
        var listId = Guid.NewGuid();

        var card = Card.Create(boardId, listId, "  Fix bug  ", Pos(), CardPriority.High, "  details  ");

        Assert.Equal(boardId, card.BoardId);
        Assert.Equal(listId, card.BoardListId);
        Assert.Equal("Fix bug", card.Title);
        Assert.Equal("details", card.Description);
        Assert.Equal(CardPriority.High, card.Priority);
        Assert.False(card.IsDeleted);
        Assert.Contains(card.DomainEvents, e => e is CardCreatedEvent);
    }

    [Fact]
    public void Create_BlankTitle_Throws()
    {
        Assert.Throws<DomainException>(() =>
            Card.Create(Guid.NewGuid(), Guid.NewGuid(), "   ", Pos()));
    }

    [Fact]
    public void MoveTo_DifferentBoard_Throws()
    {
        var card = Card.Create(Guid.NewGuid(), Guid.NewGuid(), "Card", Pos());

        Assert.Throws<DomainException>(() =>
            card.MoveTo(Guid.NewGuid(), Guid.NewGuid(), FractionalIndex.Start()));
    }

    [Fact]
    public void MoveTo_SameBoard_UpdatesListAndPositionAndRaisesEvent()
    {
        var boardId = Guid.NewGuid();
        var card = Card.Create(boardId, Guid.NewGuid(), "Card", Pos());
        card.ClearDomainEvents();

        var targetList = Guid.NewGuid();
        var newPos = FractionalIndex.Between(card.Position, null);
        card.MoveTo(boardId, targetList, newPos);

        Assert.Equal(targetList, card.BoardListId);
        Assert.Equal(newPos.Value, card.Position.Value);
        Assert.Contains(card.DomainEvents, e => e is CardMovedEvent);
    }

    [Fact]
    public void Update_TooLongTitle_Throws()
    {
        var card = Card.Create(Guid.NewGuid(), Guid.NewGuid(), "Card", Pos());
        var longTitle = new string('x', 201);

        Assert.Throws<DomainException>(() => card.Update(longTitle, null, CardPriority.None));
    }

    [Fact]
    public void SoftDelete_SetsFlag()
    {
        var card = Card.Create(Guid.NewGuid(), Guid.NewGuid(), "Card", Pos());
        card.SoftDelete();
        Assert.True(card.IsDeleted);
    }
}
