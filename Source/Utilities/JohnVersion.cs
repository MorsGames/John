using System.Reflection;

namespace John;

internal static class JohnVersion
{
    // Amen
    public static string Current => typeof(JohnVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
}
