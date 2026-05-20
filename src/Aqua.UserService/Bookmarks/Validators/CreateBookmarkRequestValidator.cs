using Aqua.UserService.Bookmarks.Dto;
using FluentValidation;

namespace Aqua.UserService.Bookmarks.Validators;

public sealed class CreateBookmarkRequestValidator : AbstractValidator<CreateBookmarkRequest>
{
    public CreateBookmarkRequestValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.ItemType).NotEmpty().MaximumLength(64);
        RuleFor(x => x.ItemId).GreaterThan(0);
        RuleFor(x => x.Label).MaximumLength(255);
    }
}
