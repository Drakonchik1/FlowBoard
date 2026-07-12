using FlowBoard.Application.Features.Tags;
using FlowBoard.Application.Features.Tags.Commands.ApplyTagToCard;
using FlowBoard.Application.Features.Tags.Commands.CreateTag;
using FlowBoard.Application.Features.Tags.Commands.DeleteTag;
using FlowBoard.Application.Features.Tags.Commands.RemoveTagFromCard;
using FlowBoard.Application.Features.Tags.Commands.UpdateTag;
using FlowBoard.Application.Features.Tags.Queries.GetTagById;
using FlowBoard.Application.Features.Tags.Queries.GetTagsByCard;
using FlowBoard.Application.Features.Tags.Queries.GetTagsByWorkspace;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowBoard.API.Controllers;

[ApiController]
[Authorize]
public sealed class TagsController(ISender sender) : ControllerBase
{
    /// <summary>List all tags in a workspace. Workspace members only.</summary>
    [HttpGet("api/workspaces/{workspaceId:guid}/tags")]
    [ProducesResponseType(typeof(IReadOnlyList<TagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListByWorkspace(Guid workspaceId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetTagsByWorkspaceQuery(workspaceId), cancellationToken));

    /// <summary>Create a tag in a workspace. Requires write access.</summary>
    [HttpPost("api/workspaces/{workspaceId:guid}/tags")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        Guid workspaceId,
        [FromBody] CreateTagPayload payload,
        CancellationToken cancellationToken)
    {
        var tag = await sender.Send(
            new CreateTagCommand(workspaceId, payload.Name, payload.Color), cancellationToken);
        return CreatedAtRoute("GetTagById", new { id = tag.Id }, tag);
    }

    /// <summary>Get a single tag. Workspace members only.</summary>
    [HttpGet("api/tags/{id:guid}", Name = "GetTagById")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetTagByIdQuery(id), cancellationToken));

    /// <summary>Update a tag. Requires write access.</summary>
    [HttpPatch("api/tags/{id:guid}")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTagPayload payload,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateTagCommand(id, payload.Name, payload.Color), cancellationToken));

    /// <summary>Soft-delete a tag and remove it from all cards. Requires write access.</summary>
    [HttpDelete("api/tags/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteTagCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>List tags applied to a card. Workspace members only.</summary>
    [HttpGet("api/cards/{cardId:guid}/tags")]
    [ProducesResponseType(typeof(IReadOnlyList<TagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListByCard(Guid cardId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetTagsByCardQuery(cardId), cancellationToken));

    /// <summary>Apply a workspace tag to a card. Requires write access.</summary>
    [HttpPut("api/cards/{cardId:guid}/tags/{tagId:guid}")]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApplyToCard(
        Guid cardId, Guid tagId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ApplyTagToCardCommand(cardId, tagId), cancellationToken));

    /// <summary>Remove a tag from a card. Requires write access.</summary>
    [HttpDelete("api/cards/{cardId:guid}/tags/{tagId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromCard(
        Guid cardId, Guid tagId, CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveTagFromCardCommand(cardId, tagId), cancellationToken);
        return NoContent();
    }
}

public sealed record CreateTagPayload(string Name, string? Color);
public sealed record UpdateTagPayload(string Name, string? Color);
