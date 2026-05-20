using Aqua.UserService.Users.Dto;
using FluentValidation;

namespace Aqua.UserService.Users.Validators;

public sealed class PatchUserRequestValidator : AbstractValidator<PatchUserRequest>
{
    public PatchUserRequestValidator()
    {
        When(x => x.Email is not null, () => RuleFor(x => x.Email).EmailAddress());
        RuleFor(x => x.FirstName).MaximumLength(255);
        RuleFor(x => x.Surname).MaximumLength(255);
        RuleFor(x => x.Version).GreaterThanOrEqualTo(0);
    }
}
