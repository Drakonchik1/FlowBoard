using FlowBoard.Application.Features.Boards;
using MediatR;

namespace FlowBoard.Application.Features.Boards.Commands.CreateBoard;

public sealed record CreateBoardCommand(Guid ProjectId, string Name) : IRequest<BoardDto>;
