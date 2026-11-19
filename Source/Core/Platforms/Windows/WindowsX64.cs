namespace John;

internal sealed class WindowsX64 : WindowsPlatform
{
    public override string Name => "WindowsX64";
    public override string DisplayName => "Windows x64";
    protected override bool IsX86 => false;
}
