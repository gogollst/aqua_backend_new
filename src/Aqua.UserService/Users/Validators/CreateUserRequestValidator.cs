using Aqua.UserService.Users.Dto;
using FluentValidation;

namespace Aqua.UserService.Users.Validators;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(255).Matches(@"^[a-zA-Z0-9._-]+$");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Surname).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Phone).MaximumLength(64);
        RuleFor(x => x.Position).MaximumLength(255);
    }
}
