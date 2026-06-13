using FlowBoard.Application.Features.BoardLists;
using MediatR;

namespace FlowBoard.Application.Features.BoardLists.Commands.CreateBoardList;

public sealed record CreateBoardListCommand(Guid BoardId, string Name) : IRequest<BoardListDto>;
