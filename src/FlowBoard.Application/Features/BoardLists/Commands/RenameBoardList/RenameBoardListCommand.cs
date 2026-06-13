using FlowBoard.Application.Features.BoardLists;
using MediatR;

namespace FlowBoard.Application.Features.BoardLists.Commands.RenameBoardList;

public sealed record RenameBoardListCommand(Guid ListId, string Name) : IRequest<BoardListDto>;
