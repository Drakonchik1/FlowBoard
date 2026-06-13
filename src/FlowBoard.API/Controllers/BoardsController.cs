using FlowBoard.Application.Features.Boards;
using FlowBoard.Application.Features.Boards.Commands.CreateBoard;
using FlowBoard.Application.Features.Boards.Commands.DeleteBoard;
using FlowBoard.Application.Features.Boards.Commands.UpdateBoard;
using FlowBoard.Application.Features.Boards.Queries.GetBoard;
using FlowBoard.Application.Features.Boards.Queries.GetBoardsInProject;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowBoard.API.Controllers;

[ApiController]
[Authorize]
public sealed class BoardsController(ISender sender) : ControllerBase
{
    /// <summary>List all boards in a project. Workspace members only.</summary>
    [HttpGet("api/projects/{projectId:guid}/boards")]
    [ProducesResponseType(typeof(IReadOnlyList<BoardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForProject(Guid projectId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetBoardsInProjectQuery(projectId), cancellationToken));

    /// <summary>Get the full Kanban board with lists and cards (Dapper read). Workspace members only.</summary>
    [HttpGet("api/boards/{id:guid}", Name = "GetBoardById")]
    [ProducesResponseType(typeof(BoardViewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetBoardQuery(id), cancellationToken));

    /// <summary>Create a board in a project. Requires write access.</summary>
    [HttpPost("api/projects/{projectId:guid}/boards")]
    [ProducesResponseType(typeof(BoardDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        Guid projectId,
        [FromBody] CreateBoardPayload payload,
        CancellationToken cancellationToken)
    {
        var board = await sender.Send(new CreateBoardCommand(projectId, payload.Name), cancellationToken);
        return CreatedAtRoute("GetBoardById", new { id = board.Id }, board);
    }

    /// <summary>Rename a board. Requires write access.</summary>
    [HttpPatch("api/boards/{id:guid}")]
    [ProducesResponseType(typeof(BoardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateBoardPayload payload,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateBoardCommand(id, payload.Name), cancellationToken));

    /// <summary>Soft-delete a board. Requires write access.</summary>
    [HttpDelete("api/boards/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteBoardCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record CreateBoardPayload(string Name);
public sealed record UpdateBoardPayload(string Name);
