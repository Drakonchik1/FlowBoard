using FlowBoard.Domain.Common;
using FlowBoard.Domain.Events;
using FlowBoard.Domain.Exceptions;

namespace FlowBoard.Domain.Entities;

/// <summary>
/// A project groups boards inside a workspace. Aggregate root; references its owning workspace by id.
/// All mutations go through named methods — no public setters.
/// </summary>
public sealed class Project : Entity
{
    private const int NameMaxLength = 100;
    private const int DescriptionMaxLength = 1000;

    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Required by EF Core
    private Project() { }

    public static Project Create(Guid workspaceId, string name, string? description)
    {
        if (workspaceId == Guid.Empty)
            throw new DomainException("Project must belong to a workspace.");

        ValidateName(name);
        ValidateDescription(description);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        project.Raise(new ProjectCreatedEvent(project.Id, workspaceId, project.Name));
        return project;
    }

    public void Rename(string newName)
    {
        ValidateName(newName);
        Name = newName.Trim();
        Touch();
    }

    public void UpdateDescription(string? description)
    {
        ValidateDescription(description);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Touch();
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Project name cannot be empty.");

        if (name.Length > NameMaxLength)
            throw new DomainException($"Project name cannot exceed {NameMaxLength} characters.");
    }

    private static void ValidateDescription(string? description)
    {
        if (description is not null && description.Length > DescriptionMaxLength)
            throw new DomainException($"Project description cannot exceed {DescriptionMaxLength} characters.");
    }
}
