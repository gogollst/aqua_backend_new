namespace Aqua.UserService.Ldap.Dto;

public sealed record CreateLdapMappingRequest(string LdapGroupDn, long RoleId);
