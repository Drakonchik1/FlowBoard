namespace FlowBoard.Domain.Entities;

/// <summary>
/// Role of a user inside a workspace. Stored as a string in the database (not int)
/// so adding new roles never reorders existing rows and migrations stay readable.
/// </summary>
public enum WorkspaceMemberRole
{
    /// <summary>Full control: rename, delete, manage all members and content. Cannot be removed.</summary>
    Owner = 0,
    /// <summary>Manage members and content. Cannot delete the workspace or remove the Owner.</summary>
    Admin = 1,
    /// <summary>Create and edit content. Cannot manage members.</summary>
    Member = 2,
    /// <summary>Read-only access.</summary>
    Viewer = 3,
}
