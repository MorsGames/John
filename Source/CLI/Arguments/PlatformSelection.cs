namespace John;

internal sealed record AllPlatforms;

internal union PlatformSelection(Platform, AllPlatforms);
