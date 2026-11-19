namespace John;

internal sealed class JohnException(string message, int exitCode = 1) : Exception(message)
{
    public int ExitCode { get; } = exitCode;
}
