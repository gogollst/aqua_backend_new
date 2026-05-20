using Aqua.UserService.Infrastructure.Authorization;
using Aqua.UserService.Ldap.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aqua.UserService.Ldap;

[ApiController]
[Route("internal/v1/ldap")]
[Authorize(AuthenticationSchemes = InternalApiAuthHandler.SchemeName)]
public sealed class InternalLdapController : ControllerBase
{
    private readonly ILdapJitSyncer _syncer;
    public InternalLdapController(ILdapJitSyncer syncer) => _syncer = syncer;

    [HttpPost("jit-sync")]
    public async Task<ActionResult<LdapJitSyncResponse>> JitSync(LdapJitSyncRequest req)
        => Ok(await _syncer.SyncAsync(req));
}
