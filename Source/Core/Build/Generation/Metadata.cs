namespace John;

internal static class Metadata
{
    public static Dictionary<string, string> GetValues(BuildContext context, Game game, Func<string, string> escape)
    {
        return new Dictionary<string, string>
        {
            ["JOHN_TARGET"] = escape(game.Name),
            ["JOHN_DISPLAY_NAME"] = escape(game.DisplayName),
            ["JOHN_COMPANY_NAME"] = escape(game.Company),
            ["JOHN_DESCRIPTION"] = escape(game.Description),
            ["JOHN_VERSION"] = escape(game.Version.ToString()),
            ["JOHN_PLATFORM"] = escape(context.Platform.Name),
            ["JOHN_METADATA_DIR"] = escape(context.Paths.GetMetadataDirectory(game)),
        };
    }
}
