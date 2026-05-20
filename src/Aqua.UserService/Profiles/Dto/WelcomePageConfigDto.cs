namespace Aqua.UserService.Profiles.Dto;

public sealed record WelcomePageConfigDto(long Id, long UserId, string ConfigJson, long Version);

public sealed record PutWelcomePageConfigRequest(string ConfigJson, long Version);
