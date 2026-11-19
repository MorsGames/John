namespace John;

internal sealed class MacOSUniversal : MacOSPlatform
{
    public override string Name => "macOSUniversal";
    public override string DisplayName => "macOS Universal";
    protected override string Architectures => "x86_64;arm64";
}
