using FluentValidation;

namespace FlowBoard.Application.Features.Boards.Commands.CreateBoard;

public sealed class CreateBoardCommandValidator : AbstractValidator<CreateBoardCommand>
{
    public CreateBoardCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Board name is required.")
            .MaximumLength(100);
    }
}
