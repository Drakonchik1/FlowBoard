using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Interfaces;
using FlowBoard.Domain.ValueObjects;
using MediatR;

namespace FlowBoard.Application.Features.Workspaces.Commands.CreateWorkspace;

/// <summary>
/// Creates a workspace with the authenticated user as Owner.
/// If the requested slug is taken, returns a 422 ValidationException so the client can prompt
/// for a different slug rather than silently appending a suffix.
/// </summary>
public sealed class CreateWorkspaceCommandHandler(
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<CreateWorkspaceCommand, WorkspaceDto>
{
    public async Task<WorkspaceDto> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var ownerId = currentUser.UserId
            ?? throw new UnauthorizedException("You must be authenticated to create a workspace.");

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? WorkspaceSlug.FromName(request.Name)
            : WorkspaceSlug.Create(request.Slug);

        var slugTaken = await workspaceRepository.SlugExistsAsync(slug.Value, cancellationToken);
        if (slugTaken)
            throw new ValidationException([new ValidationError("Slug", "Workspace slug is already taken.")]);

        var workspace = Workspace.Create(request.Name, slug, ownerId);
        await workspaceRepository.AddAsync(workspace, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new WorkspaceDto(
            workspace.Id,
            workspace.Name,
            workspace.Slug.Value,
            workspace.OwnerId,
            workspace.CreatedAt,
            MemberCount: workspace.Members.Count);
    }
}