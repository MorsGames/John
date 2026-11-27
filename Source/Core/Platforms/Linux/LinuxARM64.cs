using System.Runtime.InteropServices;

namespace John;

internal sealed class LinuxARM64 : LinuxPlatform
{
    public override string Name => "LinuxARM64";
    public override string DisplayName => "Linux ARM64";
    protected override Architecture Architecture => Architecture.Arm64;
    protected override string AppImageArchitecture => "aarch64";
}
