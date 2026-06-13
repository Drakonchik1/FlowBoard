using FlowBoard.Application.Features.Boards;
using MediatR;

namespace FlowBoard.Application.Features.Boards.Commands.UpdateBoard;

public sealed record UpdateBoardCommand(Guid BoardId, string Name) : IRequest<BoardDto>;
