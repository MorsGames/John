namespace John;

internal sealed class Project
{
    public required Manifest Manifest { get; init; }
    public required ProjectPaths Paths { get; init; }

    public required IReadOnlyList<Configuration> Configurations { get; init; }
    public required IReadOnlyList<Game> Games { get; init; }

    public static Project Load(string root)
    {
        // Paths
        var paths = new ProjectPaths(root);
        
        // john.json
        var manifest = Manifest.Load(paths.ManifestFile);
        var defaults = manifest.Defaults ?? new GameSettings();

        // Configurations
        var configurations = new List<Configuration>(Configuration.BuiltIn);
        foreach (var (name, baseConfiguration) in manifest.Configurations)
        {
            if (configurations.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                throw new JohnException($"Configuration {name} already exists!");

            configurations.Add(new Configuration(name, baseConfiguration));
        }

        // Games
        var games = new List<Game>();
        foreach (var (name, settings) in manifest.Games)
        {
            if (games.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new JohnException($"Game {name} already exists!");
            }

            // Directory
            var directory = Path.GetFullPath(Path.Combine(paths.Root, settings.Directory ?? defaults.Directory ?? "."));

            // Display Name
            var displayName = settings.DisplayName ?? defaults.DisplayName ?? name;

            // Description
            var description = settings.Description ?? defaults.Description ?? displayName;

            // Company
            var company = settings.Company ?? defaults.Company;
            if (company == null)
                throw new JohnException($"{name} needs a company! Set \"company\" for it in John.json.");

            // Version
            var versionText = settings.Version ?? defaults.Version ?? "0.0.0";
            if (!Version.TryParse(versionText, out var version))
                throw new JohnException($"{name} has an invalid version number! {versionText}");

            // CLI
            var cli = settings.CLI ?? defaults.CLI ?? false;

            // Variables
            var variables = new Dictionary<string, string>(defaults.Variables ?? []);
            foreach (var (variable, value) in settings.Variables ?? [])
            {
                variables[variable] = value;
            }

            // Resources
            List<ResourceSettings> resources = [.. defaults.Resources ?? [], .. settings.Resources ?? []];
            foreach (var resource in resources)
            {
                if (resource.From == null && resource.Create == null)
                    throw new JohnException($"{name} has a resource without \"from\" or \"create\"!");
                if (resource.From != null && resource.Create != null)
                    throw new JohnException($"{name} has a resource with both \"from\" and \"create\"! Pick one!");
            }

            // Add the game
            games.Add(new Game
            {
                Name = name,
                Directory = directory,
                DisplayName = displayName,
                Description = description,
                Company = company,
                Version = version,
                CLI = cli,
                Variables = variables,
                Resources = resources,
            });
        }

        // We're done here
        return new Project
        {
            Manifest = manifest,
            Paths = paths,
            Configurations = configurations,
            Games = games,
        };
    }

    public Configuration? FindConfiguration(string name)
    {
        return Configurations.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public Game? FindGame(string name)
    {
        return Games.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public List<string> ConfigurationNames()
    {
        var names = new List<string>();
        foreach (var configuration in Configurations)
        {
            names.Add(configuration.Name);
        }
        return names;
    }

    public List<string> GameNames()
    {
        var names = new List<string>();
        foreach (var game in Games)
        {
            names.Add(game.Name);
        }
        return names;
    }
}
