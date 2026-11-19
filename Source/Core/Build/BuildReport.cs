using Spectre.Console;

namespace John;

internal static class BuildReport
{
    private const int MAX_SLICES = 5;
    private const string OTHER = "Other";

    private static readonly Color[] SliceColors = [Color.Fuchsia, Color.Lime, Color.Yellow, Color.Aqua, Color.Orange1, Color.MediumPurple];

    public static void Show(BuildContext context, IEnumerable<Game> games, BuildTimer timer)
    {
        Log.Section("Build Report");

        // The timing section
        Log.Echo($"  [{Theme.NAME}]Build Time:[/]  {Log.Duration(timer.Elapsed)}");
        var width = Math.Min(Log.Terminal.Profile.Width - 4, 80);
        var phases = new List<string>();
        var timeChart = new BreakdownChart().Width(width).UseValueFormatter(x => Log.Duration(TimeSpan.FromMilliseconds(x)));
        foreach (var phase in Enum.GetValues<BuildPhase>())
        {
            var duration = timer.Duration(phase);
            if (duration < TimeSpan.FromMilliseconds(50))
                continue;
            timeChart.AddItem(phase.ToString(), duration.TotalMilliseconds, SliceColors[phases.Count % SliceColors.Length]);
            phases.Add($"{phase} {Log.Duration(duration)}");
        }

        if (Log.Fancy)
            Log.Terminal.Write(new Padder(timeChart, new Padding(2, 0, 0, 0)));
        else
            Log.Info($"  {string.Join(", ", phases)}");

        // The size section
        foreach (var game in games)
        {
            var output = context.GetLayout(game);
            if (!output.IsBuilt)
                continue;

            // Measure the files
            var root = Path.GetFullPath(output.BundleDirectory ?? output.OutputDirectory);
            var executable = Path.GetFullPath(output.ProgramFile);
            var resources = Path.GetFullPath(output.ResourceDirectory);
            var isBundle = PathHelper.ContainsPath(root, resources);
            var sizes = new Dictionary<string, long>();
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                string name;
                if (file == executable)
                {
                    name = Path.GetFileName(file);
                }
                else if (PathHelper.ContainsPath(resources, file))
                {
                    name = PathHelper.TopLevelEntry(resources, file);
                }
                else if (isBundle)
                {
                    name = OTHER;
                }
                else
                {
                    name = PathHelper.TopLevelEntry(root, file);
                }
                sizes[name] = sizes.GetValueOrDefault(name) + new FileInfo(file).Length;
            }

            // Slice it up
            var other = sizes.GetValueOrDefault(OTHER);
            sizes.Remove(OTHER);
            var slices = new List<(string Name, long Size)>();
            foreach (var (name, size) in sizes)
            {
                slices.Add((name, size));
            }
            slices.Sort((first, second) => second.Size.CompareTo(first.Size));

            // No point in an "Other" section with just one thing in it
            var everythingFits = other == 0 && slices.Count <= MAX_SLICES + 1;
            while (!everythingFits && slices.Count > MAX_SLICES)
            {
                other += slices[^1].Size;
                slices.RemoveAt(slices.Count - 1);
            }
            if (other > 0)
                slices.Add((OTHER, other));

            long total = 0;
            foreach (var slice in slices)
            {
                total += slice.Size;
            }

            Log.Info("");
            Log.Echo($"  [{Theme.NAME}]{Markup.Escape(game.Name)}:[/]  {Log.Size(total)}  [{Theme.MUTED}]{Log.PathMarkup(root)}[/]");
            if (total == 0)
                continue;

            if (Log.Fancy)
            {
                var chart = new BreakdownChart().Width(width).UseValueFormatter(x => Log.Size((long)x));
                for (var i = 0; i < slices.Count; i++)
                {
                    var color = SliceColors[i % SliceColors.Length];
                    if (slices[i].Name == OTHER)
                        color = Color.Grey;
                    chart.AddItem(slices[i].Name, slices[i].Size, color);
                }
                Log.Terminal.Write(new Padder(chart, new Padding(2, 0, 0, 0)));
            }
            else
            {
                var breakdown = new List<string>();
                foreach (var slice in slices)
                {
                    breakdown.Add($"{slice.Name} {Log.Size(slice.Size)}");
                }
                Log.Info($"  {string.Join(", ", breakdown)}");
            }
        }
    }
}
