using Aqua.IdentityService.Authentication;
using Aqua.IdentityService.Domain;
using FluentAssertions;
using Xunit;

namespace Aqua.IdentityService.Tests.Authentication;

public class DatabaseAuthenticationProviderTests
{
    private sealed class FakeUserRepo : IUserRepository
    {
        public AquaUser? UserToReturn;
        public AquaUserPassword? PasswordToReturn;
        public Task<AquaUser?> FindByUserNameAsync(string un, CancellationToken ct) => Task.FromResult(UserToReturn);
        public Task<AquaUserPassword?> GetPasswordForAsync(int userId, CancellationToken ct) => Task.FromResult(PasswordToReturn);
        public Task<AquaUser?> GetByIdAsync(int userId, CancellationToken ct) => Task.FromResult(UserToReturn);
        public Task IncrementFailedLoginAsync(int userId, CancellationToken ct) => Task.CompletedTask;
        public Task ResetFailedLoginAsync(int userId, CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public async Task UnknownUser_FailsWithReason()
    {
        var repo = new FakeUserRepo();
        var sut = new DatabaseAuthenticationProvider(repo);
        var result = await sut.AuthenticateAsync("alice", "secret");
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be(AuthenticationFailureReason.UnknownUser);
    }

    [Fact]
    public async Task DeletedUser_FailsWithAccountDeleted()
    {
        var repo = new FakeUserRepo
        {
            UserToReturn = new AquaUser { UserName = "alice", Deleted = true },
            PasswordToReturn = new AquaUserPassword(),
        };
        var sut = new DatabaseAuthenticationProvider(repo);
        var result = await sut.AuthenticateAsync("alice", "secret");
        result.FailureReason.Should().Be(AuthenticationFailureReason.AccountDeleted);
    }

    [Fact]
    public async Task CorrectBcryptHash_Succeeds()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("hunter2");
        var user = new AquaUser { UserName = "alice" };
        typeof(AquaUser).GetProperty("Id")!.SetValue(user, 42);
        var repo = new FakeUserRepo
        {
            UserToReturn = user,
            PasswordToReturn = new AquaUserPassword { Password = hash },
        };
        var sut = new DatabaseAuthenticationProvider(repo);
        var result = await sut.AuthenticateAsync("alice", "hunter2");
        result.Success.Should().BeTrue();
        result.UserId.Should().Be(42);
    }

    [Fact]
    public async Task WrongPassword_FailsWithReason()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("hunter2");
        var repo = new FakeUserRepo
        {
            UserToReturn = new AquaUser { UserName = "alice" },
            PasswordToReturn = new AquaUserPassword { Password = hash },
        };
        var sut = new DatabaseAuthenticationProvider(repo);
        var result = await sut.AuthenticateAsync("alice", "WRONG");
        result.FailureReason.Should().Be(AuthenticationFailureReason.WrongPassword);
    }
}
