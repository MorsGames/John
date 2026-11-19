namespace John;

internal sealed class GladSettings
{
    public required string API { get; set; }

    public GladProfile Profile { get; set; } = GladProfile.Compatibility;

    public string Output { get; set; } = "Libraries/glad";

    public string Extensions { get; set; } = "";
}
