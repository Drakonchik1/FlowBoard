using FlowBoard.Application.Features.BoardLists;
using MediatR;

namespace FlowBoard.Application.Features.BoardLists.Commands.MoveBoardList;

/// <summary>
/// Reorders a list among its siblings. The client supplies the two neighbours the list is dropped
/// between: <paramref name="BeforeListId"/> (null = move to the start) and
/// <paramref name="AfterListId"/> (null = move to the end).
/// </summary>
public sealed record MoveBoardListCommand(
    Guid ListId,
    Guid? BeforeListId,
    Guid? AfterListId) : IRequest<BoardListDto>;
