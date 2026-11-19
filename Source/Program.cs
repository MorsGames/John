using System.Text;

namespace John;

internal static class Program
{
    public static int Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
            return Run(args);

        var original = Console.OutputEncoding;
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            return Run(args);
        }
        finally
        {
            Console.OutputEncoding = original;
        }
    }

    private static int Run(string[] args)
    {
        try
        {
            return CLI.Run(args);
        }
        catch (JohnException e)
        {
            Log.Error(e.Message);
            return e.ExitCode;
        }
        catch (Exception e)
        {
            Log.Crash(e);
            return 1;
        }
    }
}
