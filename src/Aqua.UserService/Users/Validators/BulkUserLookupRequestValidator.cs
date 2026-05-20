using Aqua.UserService.Users.Dto;
using FluentValidation;

namespace Aqua.UserService.Users.Validators;

public sealed class BulkUserLookupRequestValidator : AbstractValidator<BulkUserLookupRequest>
{
    public BulkUserLookupRequestValidator()
    {
        RuleFor(x => x.Ids).NotEmpty().Must(ids => ids.Count <= 200)
            .WithMessage("At most 200 ids per request.");
    }
}
