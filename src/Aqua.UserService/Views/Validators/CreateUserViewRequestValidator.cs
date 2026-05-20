using Aqua.UserService.Views.Dto;
using FluentValidation;

namespace Aqua.UserService.Views.Validators;

public sealed class CreateUserViewRequestValidator : AbstractValidator<CreateUserViewRequest>
{
    public CreateUserViewRequestValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ConfigJson).MaximumLength(65535);
    }
}
