namespace FlowBoard.Domain.Entities;

/// <summary>
/// Card priority. Stored as a string in the database (not int) so adding new levels never
/// reorders existing rows and migrations stay readable.
/// </summary>
public enum CardPriority
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
}
