using FluentValidation;

namespace FlowBoard.Application.Features.BoardLists.Commands.RenameBoardList;

public sealed class RenameBoardListCommandValidator : AbstractValidator<RenameBoardListCommand>
{
    public RenameBoardListCommandValidator()
    {
        RuleFor(x => x.ListId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("List name is required.")
            .MaximumLength(100);
    }
}
