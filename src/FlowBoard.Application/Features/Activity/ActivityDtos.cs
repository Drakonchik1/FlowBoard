namespace FlowBoard.Application.Features.Activity;

public sealed record ActivityLogDto(
    Guid Id,
    string Type,
    Guid ActorId,
    Guid? TargetUserId,
    string? TargetRole,
    Guid? FromListId,
    Guid? ToListId,
    DateTime CreatedAt);
