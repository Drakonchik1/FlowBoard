using FluentValidation;

namespace FlowBoard.Application.Features.Tags.Commands.CreateTag;

public sealed class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tag name is required.")
            .MaximumLength(50);

        RuleFor(x => x.Color)
            .MaximumLength(7)
            .When(x => x.Color is not null);
    }
}
