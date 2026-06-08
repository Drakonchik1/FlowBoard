using FlowBoard.Application.Features.Workspaces;
using FlowBoard.Application.Features.Workspaces.Commands.ChangeMemberRole;
using FlowBoard.Application.Features.Workspaces.Commands.CreateWorkspace;
using FlowBoard.Application.Features.Workspaces.Commands.DeleteWorkspace;
using FlowBoard.Application.Features.Workspaces.Commands.InviteMember;
using FlowBoard.Application.Features.Workspaces.Commands.RemoveMember;
using FlowBoard.Application.Features.Workspaces.Commands.UpdateWorkspace;
using FlowBoard.Application.Features.Workspaces.Queries.GetMyWorkspaces;
using FlowBoard.Application.Features.Workspaces.Queries.GetWorkspaceById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowBoard.API.Controllers;

[ApiController]
[Route("api/workspaces")]
[Authorize]
public sealed class WorkspacesController(ISender sender) : ControllerBase
{
    /// <summary>List all workspaces the authenticated user belongs to.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WorkspaceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyWorkspaces(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetMyWorkspacesQuery(), cancellationToken));

    /// <summary>Get full workspace details including members. Members only.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkspaceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetWorkspaceByIdQuery(id), cancellationToken));

    /// <summary>Create a new workspace. Caller becomes the Owner.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(WorkspaceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateWorkspaceCommand command,
        CancellationToken cancellationToken)
    {
        var workspace = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = workspace.Id }, workspace);
    }

    /// <summary>Rename a workspace (name only). Owner or Admin only.</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(WorkspaceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateWorkspacePayload payload,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateWorkspaceCommand(id, payload.Name), cancellationToken);
        return Ok(result);
    }

    /// <summary>Soft-delete a workspace. Owner only.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteWorkspaceCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Invite a user by UserId with the given role. Owner or Admin only.</summary>
    [HttpPost("{id:guid}/members")]
    [ProducesResponseType(typeof(WorkspaceMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InviteMember(
        Guid id,
        [FromBody] InviteMemberPayload payload,
        CancellationToken cancellationToken)
    {
        var member = await sender.Send(
            new InviteMemberCommand(id, payload.UserId, payload.Role), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, member);
    }

    /// <summary>Remove a member, or leave the workspace by passing your own UserId.</summary>
    [HttpDelete("{id:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(
        Guid id, Guid userId, CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveMemberCommand(id, userId), cancellationToken);
        return NoContent();
    }

    /// <summary>Change a member's role. Owner or Admin only.</summary>
    [HttpPatch("{id:guid}/members/{userId:guid}")]
    [ProducesResponseType(typeof(WorkspaceMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeMemberRole(
        Guid id, Guid userId,
        [FromBody] ChangeMemberRolePayload payload,
        CancellationToken cancellationToken)
    {
        var member = await sender.Send(
            new ChangeMemberRoleCommand(id, userId, payload.NewRole), cancellationToken);
        return Ok(member);
    }
}

public sealed record UpdateWorkspacePayload(string Name);
public sealed record InviteMemberPayload(Guid UserId, WorkspaceRole Role);
public sealed record ChangeMemberRolePayload(WorkspaceRole NewRole);
