namespace Aqua.UserService.Ldap.Dto;

public sealed record LdapGroupMappingDto(long Id, long CustomerId, string LdapGroupDn, long RoleId, DateTime CreatedAt);
