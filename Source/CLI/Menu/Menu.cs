using System.CommandLine;
using Spectre.Console;

namespace John;

internal static class Menu
{
    public sealed record Quit;
    private sealed record Back;

    public union Result(string[], Quit);
    private union Choice(string, Back);

    public static Result Pick(Command root, Project? project)
    {
        // Get the commands
        var commands = new List<Command>();
        var commandChoices = new List<(string, string?)>();
        foreach (var command in root.Subcommands)
        {
            if (command is not JohnCommand)
                continue;
            if (command.Arguments.Any(x => x is not IMenuArgument && x.Arity.MinimumNumberOfValues > 0))
                continue;
            if (command.Options.Any(x => x.Required))
                continue;
            commands.Add(command);
            commandChoices.Add((command.Name, command.Description));
        }

        // Top text
        string johnText;
        if (project != null)
            johnText = Markup.Escape(Path.GetFileName(project.Paths.Root));
        else
            johnText = "No John.json found!";
        var commandTitle = $"[{Theme.ACCENT} {Theme.NAME}]John v{Markup.Escape(JohnVersion.Current)}[/] [{Theme.MUTED}]({johnText})[/]";

        // The ~~game~~ UI loop
        string? lastCommand = null;
        while (true)
        {
            // Pick a command
            if (Choose(commandTitle, commandChoices, lastCommand, quits: true) is not string commandName)
                return new Quit();
            var picked = commands.First(x => x.Name == commandName);
            lastCommand = picked.Name;

            // Get the questions
            var questions = new List<MenuQuestion>();
            foreach (var arg in picked.Arguments)
            {
                if (arg is IMenuArgument menuArgument)
                    questions.AddRange(menuArgument.Questions(project));
            }

            var answers = new string[questions.Count];
            var questionsToAsk = new List<int>();
            for (var i = 0; i < questions.Count; i++)
            {
                if (questions[i].Choices.Count == 1)
                    answers[i] = questions[i].Choices[0].Value;
                else
                    questionsToAsk.Add(i);
            }

            // Ask the questions
            var current = 0;
            while (current >= 0 && current < questionsToAsk.Count)
            {
                var index = questionsToAsk[current];
                var (title, choices, initial) = questions[index];
                switch (Choose($"[{Theme.ACCENT} {Theme.NAME}]{Markup.Escape(title)}[/]", choices, answers[index] ?? initial))
                {
                    case Back:
                        current--;
                        break;
                    case string value:
                        answers[index] = value;
                        current++;
                        break;
                }
            }
            var backedOutToCommands = current < 0;
            if (backedOutToCommands)
                continue;

            // Print the command
            string[] args = [picked.Name, .. answers];
            Log.Echo($"[{Theme.MUTED}]>[/] [{Theme.ACCENT}]john {Markup.Escape(string.Join(" ", args))}[/]");
            return args;
        }
    }

    private static Choice Choose(string title, IReadOnlyList<(string Value, string? Description)> choices, string? initial = null, bool quits = false)
    {
        var nameWidth = 0;
        foreach (var (value, _) in choices)
        {
            nameWidth = Math.Max(nameWidth, value.Length);
        }

        // Labels
        var labels = new Dictionary<Choice, string>();
        foreach (var (value, description) in choices)
        {
            var label = $"[{Theme.NAME}]{Markup.Escape(value)}[/]";
            if (!string.IsNullOrEmpty(description))
            {
                var sentence = StringHelper.FirstSentence(description);
                sentence = StringHelper.Truncate(sentence, Log.Terminal.Profile.Width - nameWidth - 6);

                var padding = new string(' ', nameWidth - value.Length + 2);
                label += $"{padding}[{Theme.MUTED}]{Markup.Escape(sentence)}[/]";
            }
            labels[value] = label;
        }

        // The back option
        var back = new Back();
        labels[back] = $"[{Theme.MUTED}]{(quits ? "Quit" : "Back")}[/]";

        // Build the actual prompt
        var prompt = new SelectionPrompt<Choice>()
            .Title(title)
            .HighlightStyle(Theme.AccentStyle.Combine(Theme.NameStyle))
            .PageSize(Math.Max(3, Math.Min(labels.Count, Log.Terminal.Profile.Height - 4)))
            .WrapAround()
            .UseConverter(x => labels[x])
            .AddChoices(labels.Keys)
            .AddCancelResult(back)
            .EnableSearch()
            .SearchPlaceholderText($"[{Theme.MUTED}](Type to search, Esc to {(quits ? "quit" : "go back")})[/]");
        if (initial != null && labels.ContainsKey(initial))
            prompt.DefaultValue(initial);
        return prompt.Show(Log.Terminal);
    }
}
