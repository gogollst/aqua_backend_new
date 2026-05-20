using Aqua.UserService.Roles.Dto;
using FluentValidation;

namespace Aqua.UserService.Roles.Validators;

public sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).MaximumLength(1024);
        RuleFor(x => x.Permissions).GreaterThanOrEqualTo(0);
    }
}
