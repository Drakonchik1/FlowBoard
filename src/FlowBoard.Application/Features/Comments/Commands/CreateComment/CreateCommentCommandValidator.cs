using FluentValidation;

namespace FlowBoard.Application.Features.Comments.Commands.CreateComment;

public sealed class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentCommandValidator()
    {
        RuleFor(x => x.CardId).NotEmpty();

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Comment body is required.")
            .MaximumLength(4000);
    }
}
