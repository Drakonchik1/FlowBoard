using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Workspaces.Queries.GetWorkspaceById;

public sealed class GetWorkspaceByIdQueryHandler(
    IWorkspaceRepository workspaceRepository,
    ICurrentUserService currentUser) : IRequestHandler<GetWorkspaceByIdQuery, WorkspaceDetailDto>
{
    public async Task<WorkspaceDetailDto> Handle(GetWorkspaceByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId
            ?? throw new UnauthorizedException("You must be authenticated.");

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException("Workspace", request.WorkspaceId);

        // Return 404 (not 403) for non-members to avoid workspace ID enumeration
        if (!workspace.HasMember(userId))
            throw new NotFoundException("Workspace", request.WorkspaceId);

        var memberDtos = workspace.Members
            .Select(m => new WorkspaceMemberDto(m.UserId, m.Role.ToString(), m.JoinedAt))
            .ToList();

        return new WorkspaceDetailDto(
            workspace.Id,
            workspace.Name,
            workspace.Slug.Value,
            workspace.OwnerId,
            workspace.CreatedAt,
            workspace.UpdatedAt,
            memberDtos);
    }
}