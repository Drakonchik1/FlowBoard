using FlowBoard.Application.Features.Projects;
using FlowBoard.Application.Features.Projects.Commands.CreateProject;
using FlowBoard.Application.Features.Projects.Commands.DeleteProject;
using FlowBoard.Application.Features.Projects.Commands.UpdateProject;
using FlowBoard.Application.Features.Projects.Queries.GetProjectById;
using FlowBoard.Application.Features.Projects.Queries.GetProjectsInWorkspace;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowBoard.API.Controllers;

[ApiController]
[Authorize]
public sealed class ProjectsController(ISender sender) : ControllerBase
{
    /// <summary>List all projects in a workspace. Workspace members only.</summary>
    [HttpGet("api/workspaces/{workspaceId:guid}/projects")]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForWorkspace(Guid workspaceId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetProjectsInWorkspaceQuery(workspaceId), cancellationToken));

    /// <summary>Get a single project. Workspace members only.</summary>
    [HttpGet("api/projects/{id:guid}", Name = "GetProjectById")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetProjectByIdQuery(id), cancellationToken));

    /// <summary>Create a project in a workspace. Requires write access.</summary>
    [HttpPost("api/workspaces/{workspaceId:guid}/projects")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        Guid workspaceId,
        [FromBody] CreateProjectPayload payload,
        CancellationToken cancellationToken)
    {
        var project = await sender.Send(
            new CreateProjectCommand(workspaceId, payload.Name, payload.Description), cancellationToken);
        return CreatedAtRoute("GetProjectById", new { id = project.Id }, project);
    }

    /// <summary>Update a project's name and description. Requires write access.</summary>
    [HttpPatch("api/projects/{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProjectPayload payload,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateProjectCommand(id, payload.Name, payload.Description), cancellationToken));

    /// <summary>Soft-delete a project. Requires write access.</summary>
    [HttpDelete("api/projects/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteProjectCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record CreateProjectPayload(string Name, string? Description);
public sealed record UpdateProjectPayload(string Name, string? Description);
