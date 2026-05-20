using Aqua.UserService.Tenants.Dto;
using FluentValidation;

namespace Aqua.UserService.Tenants.Validators;

public sealed class PatchTenantSettingsRequestValidator : AbstractValidator<PatchTenantSettingsRequest>
{
    public PatchTenantSettingsRequestValidator()
    {
        When(x => x.DisplayName is not null, () => RuleFor(x => x.DisplayName).MaximumLength(255));
        RuleFor(x => x.Version).GreaterThanOrEqualTo(0);
    }
}
