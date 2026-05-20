using Aqua.UserService.Domain;
using Aqua.UserService.Ldap;
using Aqua.UserService.Ldap.Dto;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Aqua.UserService.Tests.Ldap;

public sealed class LdapGroupMappingManagerTests
{
    private readonly ILdapGroupRoleMappingRepository _repo = Substitute.For<ILdapGroupRoleMappingRepository>();
    private readonly LdapGroupMappingManager _sut;
    public LdapGroupMappingManagerTests() => _sut = new LdapGroupMappingManager(_repo);

    [Fact]
    public async Task Create_persists_mapping_and_returns_dto()
    {
        _repo.FindAsync(1L, "cn=qa,dc=acme,dc=com", 17L)
            .Returns(Task.FromResult<LdapGroupRoleMapping?>(null));

        var dto = await _sut.CreateAsync(
            new CreateLdapMappingRequest("cn=qa,dc=acme,dc=com", 17L),
            customerId: 1L);

        dto.LdapGroupDn.Should().Be("cn=qa,dc=acme,dc=com");
        dto.RoleId.Should().Be(17L);
        await _repo.Received(1).InsertAsync(Arg.Is<LdapGroupRoleMapping>(m =>
            m.LdapGroupDn == "cn=qa,dc=acme,dc=com" && m.RoleId == 17L && m.CustomerId == 1L));
    }

    [Fact]
    public async Task Create_duplicate_throws_conflict()
    {
        _repo.FindAsync(1L, "cn=qa,dc=acme,dc=com", 17L)
            .Returns(Task.FromResult<LdapGroupRoleMapping?>(new LdapGroupRoleMapping()));

        var act = () => _sut.CreateAsync(
            new CreateLdapMappingRequest("cn=qa,dc=acme,dc=com", 17L), customerId: 1L);

        await act.Should().ThrowAsync<ConflictException>()
            .Where(e => e.ErrorCode == "ldap-mapping.duplicate");
    }

    [Fact]
    public async Task Delete_removes_mapping()
    {
        var existing = new LdapGroupRoleMapping { Id = 42L, CustomerId = 1L };
        _repo.FindByIdAsync(42L).Returns(Task.FromResult<LdapGroupRoleMapping?>(existing));

        await _sut.DeleteAsync(42L, customerId: 1L);

        await _repo.Received(1).DeleteAsync(existing);
    }
}
