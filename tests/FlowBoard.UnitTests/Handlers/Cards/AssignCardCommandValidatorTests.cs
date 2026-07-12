using FlowBoard.Application.Features.Cards.Commands.AssignCard;
using FluentValidation.TestHelper;

namespace FlowBoard.UnitTests.Handlers.Cards;

public sealed class AssignCardCommandValidatorTests
{
    private readonly AssignCardCommandValidator _validator = new();

    [Fact]
    public void Validate_EmptyAssigneeId_Fails()
    {
        var result = _validator.TestValidate(new AssignCardCommand(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(c => c.AssigneeId);
    }

    [Fact]
    public void Validate_NullAssignee_Passes()
    {
        var result = _validator.TestValidate(new AssignCardCommand(Guid.NewGuid(), null));
        result.ShouldNotHaveValidationErrorFor(c => c.AssigneeId);
    }
}
