using FluentValidation;

namespace FlowBoard.Application.Features.Cards.Commands.AssignCard;

public sealed class AssignCardCommandValidator : AbstractValidator<AssignCardCommand>
{
    public AssignCardCommandValidator()
    {
        RuleFor(c => c.CardId).NotEmpty();
        RuleFor(c => c.AssigneeId)
            .Must(id => id is null || id != Guid.Empty)
            .WithMessage("Assignee id cannot be empty.");
    }
}
