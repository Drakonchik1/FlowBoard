using MediatR;

namespace FlowBoard.Application.Features.BoardLists.Commands.DeleteBoardList;

public sealed record DeleteBoardListCommand(Guid ListId) : IRequest;
