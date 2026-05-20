using Aqua.UserService.Ldap.Dto;
using FluentValidation;

namespace Aqua.UserService.Ldap.Validators;

public sealed class LdapJitSyncRequestValidator : AbstractValidator<LdapJitSyncRequest>
{
    public LdapJitSyncRequestValidator()
    {
        RuleFor(x => x.CustomerSlug).NotEmpty();
        RuleFor(x => x.LdapDn).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Username).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Groups).NotNull();
    }
}
