namespace John;

internal sealed class WindowsX86 : WindowsPlatform
{
    public override string Name => "WindowsX86";
    public override string DisplayName => "Windows x86";
    protected override bool IsX86 => true;
}
