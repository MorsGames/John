using System.Text;

namespace John;

internal sealed record PerConfigurationBase(string Debug, string Release, string RelWithDebInfo)
{
    public static PerConfigurationBase Of(string debug, string release)
    {
        return new PerConfigurationBase(debug, release, release);
    }

    public string For(ConfigurationBase baseConfiguration)
    {
        return baseConfiguration switch
        {
            ConfigurationBase.Debug => Debug,
            ConfigurationBase.Release => Release,
            ConfigurationBase.RelWithDebInfo => RelWithDebInfo,
        };
    }

    public string For(Configuration configuration)
    {
        return For(configuration.Base);
    }

    public string Genex(Project project)
    {
        var sb = new StringBuilder();
        foreach (var baseConfiguration in Enum.GetValues<ConfigurationBase>())
        {
            var value = For(baseConfiguration);
            if (value.Length == 0)
                continue;

            var names = new List<string>();
            foreach (var configuration in project.Configurations)
            {
                if (configuration.Base == baseConfiguration)
                    names.Add(configuration.Name);
            }
            var configs = string.Join(',', names);
            sb.Append($"$<$<CONFIG:{configs}>:{value}>");
        }
        return sb.ToString();
    }
}
