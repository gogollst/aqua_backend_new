using Aqua.UserService.Tenants;
using Aqua.UserService.Tenants.Dto;
using Aqua.UserService.Tests.TestSupport;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Aqua.UserService.Tests.Tenants;

public sealed class TenantManagerTests
{
    private readonly ICustomerRepository _repo = Substitute.For<ICustomerRepository>();
    private readonly TenantManager _sut;

    public TenantManagerTests() => _sut = new TenantManager(_repo);

    [Fact]
    public async Task GetSettings_returns_existing_tenant()
    {
        var existing = new TenantBuilder().Build();
        existing.Id = 1L;
        _repo.FindByIdAsync(1L).Returns(Task.FromResult<Customer?>(existing));

        var dto = await _sut.GetSettingsAsync(1L);

        dto.Slug.Should().Be("acme");
        dto.AuthMode.Should().Be(TenantAuthMode.Local);
    }

    [Fact]
    public async Task PatchSettings_updates_display_name_and_auth_mode()
    {
        var existing = new TenantBuilder().Build();
        existing.Id = 1L;
        existing.Version = 3;
        _repo.FindByIdAsync(1L).Returns(Task.FromResult<Customer?>(existing));

        await _sut.PatchSettingsAsync(1L, new PatchTenantSettingsRequest(
            DisplayName: "Acme Corp",
            AuthMode: TenantAuthMode.Ldap,
            AuthConfigJson: """{"server":"ldap://acme.com"}""",
            Version: 3));

        existing.DisplayName.Should().Be("Acme Corp");
        existing.AuthMode.Should().Be(TenantAuthMode.Ldap);
        existing.AuthConfigJson.Should().Contain("acme.com");
    }
}
