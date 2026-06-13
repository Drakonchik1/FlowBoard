using FlowBoard.Domain.Common;
using FlowBoard.Domain.Events;
using FlowBoard.Domain.Exceptions;

namespace FlowBoard.Domain.Entities;

/// <summary>
/// A Kanban board belongs to a project. Aggregate root; references project and workspace by id.
/// WorkspaceId is denormalized from the parent project so authorization checks on board, list,
/// and card operations can resolve the workspace without traversing the full chain.
/// </summary>
public sealed class Board : Entity
{
    private const int NameMaxLength = 100;

    public Guid ProjectId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Required by EF Core
    private Board() { }

    public static Board Create(Guid projectId, Guid workspaceId, string name)
    {
        if (projectId == Guid.Empty)
            throw new DomainException("Board must belong to a project.");
        if (workspaceId == Guid.Empty)
            throw new DomainException("Board must belong to a workspace.");

        ValidateName(name);

        var board = new Board
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        board.Raise(new BoardCreatedEvent(board.Id, projectId, workspaceId));
        return board;
    }

    public void Rename(string newName)
    {
        ValidateName(newName);
        Name = newName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Board name cannot be empty.");

        if (name.Length > NameMaxLength)
            throw new DomainException($"Board name cannot exceed {NameMaxLength} characters.");
    }
}
