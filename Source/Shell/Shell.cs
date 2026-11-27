using System.Collections;
using System.Diagnostics;
using System.Text;
using CliWrap;
using CliWrap.Exceptions;

namespace John;

internal static class Shell
{
    private static readonly PipeTarget StandardOutput = PipeTarget.ToStream(Console.OpenStandardOutput());
    private static readonly PipeTarget StandardError = PipeTarget.ToStream(Console.OpenStandardError());

    public static Command Command(string program, IEnumerable<string> args, ShellEnvironment environment)
    {
        var path = environment.Find(program);
        if (path == null)
            throw new JohnException($"{program} not found!");
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (Hosts.Current == Host.Windows && extension is ".cmd" or ".bat")
        {
            var commandLine = EscapeHelper.CMD(path);
            foreach (var arg in args)
            {
                commandLine += " " + EscapeHelper.CMD(arg);
            }
            return CMD(commandLine, environment);
        }

        var variables = new Dictionary<string, string?>(Hosts.Comparer);
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            variables[(string)entry.Key] = null;
        }
        foreach (var (name, value) in environment)
        {
            variables[name] = value;
        }

        return Cli.Wrap(path)
            .WithArguments(args)
            .WithEnvironmentVariables(variables)
            .WithStandardOutputPipe(StandardOutput)
            .WithStandardErrorPipe(StandardError);
    }

    public static Command CMD(string commandLine, ShellEnvironment environment)
    {
        return Command(environment["ComSpec"], [], environment)
            .WithArguments($"/d /s /c \"{commandLine}\"");
    }

    public static int Run(this Command command, bool quiet = false)
    {
        var printsOutput = command.StandardOutputPipe == StandardOutput;
        var printsErrors = command.StandardErrorPipe == StandardError;

        quiet |= Log.IsLive && printsOutput && printsErrors;
        var lines = new List<string>();
        if (quiet)
        {
            var linesLock = new Lock();
            var pipe = PipeTarget.ToDelegate(line =>
            {
                lock (linesLock)
                {
                    lines.Add(line);
                }
            }, Console.OutputEncoding);
            command = command.WithStandardOutputPipe(pipe).WithStandardErrorPipe(pipe);
        }
        else if (Log.IsLive && printsErrors)
        {
            command = command.WithStandardErrorPipe(PipeTarget.ToDelegate(Log.Passthrough, Console.OutputEncoding));
        }

        // The child gets the Ctrl+C too
        ConsoleCancelEventHandler cancel = (_, keyPress) => keyPress.Cancel = true;
        Console.CancelKeyPress += cancel;
        try
        {
            var result = command.ExecuteAsync().GetAwaiter().GetResult();
            if (result.ExitCode != 0)
                lines.ForEach(Log.Passthrough);
            return result.ExitCode;
        }
        catch (CommandExecutionException e)
        {
            lines.ForEach(Log.Passthrough);
            throw new JohnException($"{Path.GetFileName(command.TargetFilePath)} failed with exit code {e.ExitCode}!", e.ExitCode);
        }
        finally
        {
            Console.CancelKeyPress -= cancel;
        }
    }

    public static string Capture(this Command command)
    {
        var sb = new StringBuilder();
        command.WithStandardOutputPipe(PipeTarget.ToStringBuilder(sb, Console.OutputEncoding)).Run();
        return sb.ToString();
    }

    public static void StartAndWait(this Command command, string name)
    {
        ConsoleCancelEventHandler cancel = (_, keyPress) => keyPress.Cancel = true;
        Console.CancelKeyPress += cancel;
        try
        {
            using var process = Launch(command);
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new JohnException($"{name} exited with code {process.ExitCode}!", process.ExitCode);
        }
        finally
        {
            Console.CancelKeyPress -= cancel;
        }
    }

    private static Process Launch(Command command)
    {
        var startInfo = new ProcessStartInfo(command.TargetFilePath, command.Arguments)
        {
            UseShellExecute = false,
            WorkingDirectory = command.WorkingDirPath,
        };
        foreach (var (name, value) in command.EnvironmentVariables)
        {
            if (value == null)
                startInfo.Environment.Remove(name);
            else
                startInfo.Environment[name] = value;
        }
        return Process.Start(startInfo)!;
    }
}
