namespace John;

internal sealed class FinalizeContext(BuildContext build, Game game)
{
    public BuildContext Build { get; } = build;
    public Game Game { get; } = game;
    public GameLayout Layout { get; } = build.GetLayout(game);

    public void Step(string label, IReadOnlyList<string> inputs, IReadOnlyList<string> outputs, Action run)
    {
        if (!FileHelper.IsOutdated(inputs, outputs))
            return;

        foreach (var output in outputs)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            File.Delete(output);
        }

        Log.Task(label, _ => run());
    }
}
