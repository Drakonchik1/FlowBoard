namespace FlowBoard.API.Hubs;

/// <summary>SignalR group names for board-scoped broadcasts.</summary>
internal static class BoardGroupNames
{
    public static string ForBoard(Guid boardId) => $"board:{boardId}";
}
