using FlowBoard.Application.Features.Comments;
using FlowBoard.Application.Features.Comments.Commands.CreateComment;
using FlowBoard.Application.Features.Comments.Commands.DeleteComment;
using FlowBoard.Application.Features.Comments.Commands.UpdateComment;
using FlowBoard.Application.Features.Comments.Queries.GetCommentById;
using FlowBoard.Application.Features.Comments.Queries.GetCommentsByCard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowBoard.API.Controllers;

[ApiController]
[Authorize]
public sealed class CommentsController(ISender sender) : ControllerBase
{
    /// <summary>List comments on a card. Workspace members only.</summary>
    [HttpGet("api/cards/{cardId:guid}/comments")]
    [ProducesResponseType(typeof(IReadOnlyList<CommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListByCard(Guid cardId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetCommentsByCardQuery(cardId), cancellationToken));

    /// <summary>Add a comment to a card. Requires write access.</summary>
    [HttpPost("api/cards/{cardId:guid}/comments")]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        Guid cardId,
        [FromBody] CreateCommentPayload payload,
        CancellationToken cancellationToken)
    {
        var comment = await sender.Send(new CreateCommentCommand(cardId, payload.Body), cancellationToken);
        return CreatedAtRoute("GetCommentById", new { id = comment.Id }, comment);
    }

    /// <summary>Get a single comment. Workspace members only.</summary>
    [HttpGet("api/comments/{id:guid}", Name = "GetCommentById")]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetCommentByIdQuery(id), cancellationToken));

    /// <summary>Update a comment body. Requires write access.</summary>
    [HttpPatch("api/comments/{id:guid}")]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCommentPayload payload,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateCommentCommand(id, payload.Body), cancellationToken));

    /// <summary>Soft-delete a comment. Requires write access.</summary>
    [HttpDelete("api/comments/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteCommentCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record CreateCommentPayload(string Body);
public sealed record UpdateCommentPayload(string Body);
