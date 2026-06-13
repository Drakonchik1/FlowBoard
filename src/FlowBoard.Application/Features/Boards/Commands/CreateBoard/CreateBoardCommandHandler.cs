using FlowBoard.Application.Common.Exceptions;
using FlowBoard.Application.Common.Interfaces;
using FlowBoard.Application.Common.Security;
using FlowBoard.Domain.Entities;
using FlowBoard.Domain.Exceptions;
using FlowBoard.Domain.Interfaces;
using MediatR;

namespace FlowBoard.Application.Features.Boards.Commands.CreateBoard;

public sealed class CreateBoardCommandHandler(
    IBoardRepository boardRepository,
    IProjectRepository projectRepository,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRequestHandler<CreateBoardCommand, BoardDto>
{
    public async Task<BoardDto> Handle(CreateBoardCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You must be authenticated.");

        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var workspace = await workspaceRepository.GetByIdWithMembersAsync(project.WorkspaceId, cancellationToken);
        ResourceGuard.EnsureMember(workspace, userId, "Project", request.ProjectId);
        ResourceGuard.EnsureCanWrite(workspace!, userId);

        var board = Board.Create(project.Id, project.WorkspaceId, request.Name);
        await boardRepository.AddAsync(board, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new BoardDto(
            board.Id, board.ProjectId, board.WorkspaceId, board.Name, board.CreatedAt, board.UpdatedAt);
    }
}
