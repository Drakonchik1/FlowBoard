using MediatR;

namespace FlowBoard.Application.Features.Boards.Commands.DeleteBoard;

public sealed record DeleteBoardCommand(Guid BoardId) : IRequest;
