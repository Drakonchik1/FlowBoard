using FluentValidation;
using FlowBoard.Domain.Entities;

namespace FlowBoard.Application.Features.Workspaces.Commands.ChangeMemberRole;

public sealed class ChangeMemberRoleCommandValidator : AbstractValidator<ChangeMemberRoleCommand>
{
    public ChangeMemberRoleCommandValidator()
    {
        RuleFor(x => x.NewRole)
            .IsInEnum()
            .NotEqual(WorkspaceMemberRole.Owner)
            .WithMessage("Owner role cannot be assigned via role change. Use ownership transfer.");
    }
}