using FluentValidation;

namespace FlowBoard.Application.Features.Workspaces.Commands.InviteMember;

public sealed class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator()
    {
        RuleFor(x => x.UserId).NotEqual(Guid.Empty).WithMessage("UserId is required.");
        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role.")
            .NotEqual(WorkspaceRole.Owner)
            .WithMessage("Owner role cannot be assigned via invite.");
    }
}