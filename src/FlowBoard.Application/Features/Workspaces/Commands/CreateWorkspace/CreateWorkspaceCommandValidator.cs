using FluentValidation;

namespace FlowBoard.Application.Features.Workspaces.Commands.CreateWorkspace;

public sealed class CreateWorkspaceCommandValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Workspace name is required.")
            .MaximumLength(100);

        // Slug is optional in the request — server will derive from name if missing.
        // When present, validate length only; full format check happens in the WorkspaceSlug value object.
        When(x => !string.IsNullOrWhiteSpace(x.Slug), () =>
            RuleFor(x => x.Slug!)
                .Length(3, 60).WithMessage("Slug must be between 3 and 60 characters."));
    }
}