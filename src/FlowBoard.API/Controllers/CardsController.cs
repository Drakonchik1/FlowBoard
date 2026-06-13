using FlowBoard.Application.Features.Cards;
using FlowBoard.Application.Features.Cards.Commands.CreateCard;
using FlowBoard.Application.Features.Cards.Commands.DeleteCard;
using FlowBoard.Application.Features.Cards.Commands.MoveCard;
using FlowBoard.Application.Features.Cards.Commands.UpdateCard;
using FlowBoard.Application.Features.Cards.Queries.GetCardById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowBoard.API.Controllers;

[ApiController]
[Authorize]
public sealed class CardsController(ISender sender) : ControllerBase
{
    /// <summary>Create a card at the end of a list. Requires write access.</summary>
    [HttpPost("api/lists/{listId:guid}/cards")]
    [ProducesResponseType(typeof(CardDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        Guid listId,
        [FromBody] CreateCardPayload payload,
        CancellationToken cancellationToken)
    {
        var card = await sender.Send(
            new CreateCardCommand(listId, payload.Title, payload.Description, payload.Priority), cancellationToken);
        return CreatedAtRoute("GetCardById", new { id = card.Id }, card);
    }

    /// <summary>Get a single card. Workspace members only.</summary>
    [HttpGet("api/cards/{id:guid}", Name = "GetCardById")]
    [ProducesResponseType(typeof(CardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetCardByIdQuery(id), cancellationToken));

    /// <summary>Update a card's title, description, and priority. Requires write access.</summary>
    [HttpPatch("api/cards/{id:guid}")]
    [ProducesResponseType(typeof(CardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCardPayload payload,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(
            new UpdateCardCommand(id, payload.Title, payload.Description, payload.Priority), cancellationToken));

    /// <summary>Move a card to a target list and position (drag-and-drop). Requires write access.</summary>
    [HttpPost("api/cards/{id:guid}/move")]
    [ProducesResponseType(typeof(CardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Move(
        Guid id,
        [FromBody] MoveCardPayload payload,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(
            new MoveCardCommand(id, payload.TargetListId, payload.BeforeCardId, payload.AfterCardId),
            cancellationToken));

    /// <summary>Soft-delete a card. Requires write access.</summary>
    [HttpDelete("api/cards/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteCardCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record CreateCardPayload(string Title, string? Description, CardPriority Priority = CardPriority.None);
public sealed record UpdateCardPayload(string Title, string? Description, CardPriority Priority);
public sealed record MoveCardPayload(Guid TargetListId, Guid? BeforeCardId, Guid? AfterCardId);
