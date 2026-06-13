using FluentValidation;

namespace FlowBoard.Application.Features.BoardLists.Commands.CreateBoardList;

public sealed class CreateBoardListCommandValidator : AbstractValidator<CreateBoardListCommand>
{
    public CreateBoardListCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("List name is required.")
            .MaximumLength(100);
    }
}
