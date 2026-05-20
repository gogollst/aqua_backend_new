using Aqua.UserService.Tenants.Dto;
using FluentValidation;

namespace Aqua.UserService.Tenants.Validators;

public sealed class BootstrapTenantRequestValidator : AbstractValidator<BootstrapTenantRequest>
{
    public BootstrapTenantRequestValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().Matches(@"^[a-z][a-z0-9-]{1,62}$");
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.PrimaryDomain).MaximumLength(255);
        RuleFor(x => x.AdminUser).NotNull();
        When(x => x.AdminUser is not null, () =>
        {
            RuleFor(x => x.AdminUser.Username).NotEmpty();
            RuleFor(x => x.AdminUser.Email).EmailAddress();
            RuleFor(x => x.AdminUser.PasswordMode)
                .Must(m => m == "generate" || m == "provided");
            When(x => x.AdminUser.PasswordMode == "provided",
                () => RuleFor(x => x.AdminUser.Password).NotEmpty().MinimumLength(12));
        });
        RuleFor(x => x.DefaultRoles).Must(s => s is "standard" or "minimal" or "none");
    }
}
