using FluentValidation;

namespace FlowBoard.Application.Features.Tags.Commands.UpdateTag;

public sealed class UpdateTagCommandValidator : AbstractValidator<UpdateTagCommand>
{
    public UpdateTagCommandValidator()
    {
        RuleFor(x => x.TagId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tag name is required.")
            .MaximumLength(50);

        RuleFor(x => x.Color)
            .MaximumLength(7)
            .When(x => x.Color is not null);
    }
}
