using Aqua.UserService.Ldap.Dto;
using FluentValidation;

namespace Aqua.UserService.Ldap.Validators;

public sealed class CreateLdapMappingRequestValidator : AbstractValidator<CreateLdapMappingRequest>
{
    public CreateLdapMappingRequestValidator()
    {
        RuleFor(x => x.LdapGroupDn).NotEmpty().MaximumLength(512);
        RuleFor(x => x.RoleId).GreaterThan(0);
    }
}
