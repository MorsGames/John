using System.Runtime.InteropServices;

namespace John;

internal sealed class LinuxX64 : LinuxPlatform
{
    public override string Name => "LinuxX64";
    public override string DisplayName => "Linux x64";
    protected override Architecture Architecture => Architecture.X64;
    protected override string AppImageArchitecture => "x86_64";
}
