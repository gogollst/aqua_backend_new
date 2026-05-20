using Aqua.UserService.Users.Dto;
using Aqua.UserService.Users.Validators;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.Tests.Users.Validators;

public sealed class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _v = new();

    [Fact]
    public void Empty_username_fails()
    {
        var r = _v.Validate(new CreateUserRequest("", "a@x.com", "A", "B", null, null, null));
        r.IsValid.Should().BeFalse();
        r.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Fact]
    public void Invalid_email_fails()
    {
        var r = _v.Validate(new CreateUserRequest("alice", "not-an-email", "A", "B", null, null, null));
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Valid_request_passes()
    {
        var r = _v.Validate(new CreateUserRequest("alice", "alice@x.com", "Alice", "Anderson", null, null, null));
        r.IsValid.Should().BeTrue();
    }
}
