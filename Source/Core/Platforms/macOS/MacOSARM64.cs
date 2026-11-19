namespace John;

internal sealed class MacOSARM64 : MacOSPlatform
{
    public override string Name => "macOSARM64";
    public override string DisplayName => "macOS ARM64";
    protected override string Architectures => "arm64";
}
