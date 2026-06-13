using FluentValidation;

namespace FlowBoard.Application.Features.Boards.Commands.UpdateBoard;

public sealed class UpdateBoardCommandValidator : AbstractValidator<UpdateBoardCommand>
{
    public UpdateBoardCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Board name is required.")
            .MaximumLength(100);
    }
}
