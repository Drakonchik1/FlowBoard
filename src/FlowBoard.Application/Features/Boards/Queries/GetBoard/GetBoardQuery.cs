using FlowBoard.Application.Features.Boards;
using MediatR;

namespace FlowBoard.Application.Features.Boards.Queries.GetBoard;

/// <summary>Loads the full Kanban board (lists + cards) via the Dapper read side.</summary>
public sealed record GetBoardQuery(Guid BoardId) : IRequest<BoardViewDto>;
