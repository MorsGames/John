namespace John;

internal sealed record GameLayout
{
    public required string OutputDirectory { get; init; }
    public string? BundleDirectory { get; init; }
    public required string ProgramFile { get; init; }
    public required string ResourceDirectory { get; init; }
    public string LinkedFile
    {
        get => field ?? ProgramFile;
        init;
    }
    public bool IsLinked => File.Exists(LinkedFile);
    public bool IsBuilt => File.Exists(ProgramFile);
}
