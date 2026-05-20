using Aqua.UserService.Roles.Dto;
using FluentValidation;

namespace Aqua.UserService.Roles.Validators;

public sealed class PatchRoleRequestValidator : AbstractValidator<PatchRoleRequest>
{
    public PatchRoleRequestValidator()
    {
        When(x => x.Name is not null, () => RuleFor(x => x.Name).MaximumLength(255));
        RuleFor(x => x.Version).GreaterThanOrEqualTo(0);
    }
}
