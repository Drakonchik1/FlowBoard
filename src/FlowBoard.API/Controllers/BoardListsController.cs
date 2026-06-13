using FlowBoard.Application.Features.BoardLists;
using FlowBoard.Application.Features.BoardLists.Commands.CreateBoardList;
using FlowBoard.Application.Features.BoardLists.Commands.DeleteBoardList;
using FlowBoard.Application.Features.BoardLists.Commands.MoveBoardList;
using FlowBoard.Application.Features.BoardLists.Commands.RenameBoardList;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowBoard.API.Controllers;

[ApiController]
[Authorize]
public sealed class BoardListsController(ISender sender) : ControllerBase
{
    /// <summary>Create a list (column) at the end of a board. Requires write access.</summary>
    [HttpPost("api/boards/{boardId:guid}/lists")]
    [ProducesResponseType(typeof(BoardListDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        Guid boardId,
        [FromBody] CreateBoardListPayload payload,
        CancellationToken cancellationToken)
    {
        var list = await sender.Send(new CreateBoardListCommand(boardId, payload.Name), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, list);
    }

    /// <summary>Rename a list. Requires write access.</summary>
    [HttpPatch("api/lists/{id:guid}")]
    [ProducesResponseType(typeof(BoardListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rename(
        Guid id,
        [FromBody] RenameBoardListPayload payload,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new RenameBoardListCommand(id, payload.Name), cancellationToken));

    /// <summary>Reorder a list between two sibling lists. Requires write access.</summary>
    [HttpPost("api/lists/{id:guid}/move")]
    [ProducesResponseType(typeof(BoardListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Move(
        Guid id,
        [FromBody] MoveBoardListPayload payload,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(
            new MoveBoardListCommand(id, payload.BeforeListId, payload.AfterListId), cancellationToken));

    /// <summary>Soft-delete a list. Requires write access.</summary>
    [HttpDelete("api/lists/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteBoardListCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record CreateBoardListPayload(string Name);
public sealed record RenameBoardListPayload(string Name);
public sealed record MoveBoardListPayload(Guid? BeforeListId, Guid? AfterListId);
