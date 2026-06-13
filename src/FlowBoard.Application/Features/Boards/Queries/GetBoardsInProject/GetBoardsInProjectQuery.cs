using FlowBoard.Application.Features.Boards;
using MediatR;

namespace FlowBoard.Application.Features.Boards.Queries.GetBoardsInProject;

public sealed record GetBoardsInProjectQuery(Guid ProjectId) : IRequest<IReadOnlyList<BoardDto>>;
