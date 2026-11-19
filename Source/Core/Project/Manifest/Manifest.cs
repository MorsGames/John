using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace John;

internal sealed class Manifest
{
    public List<string> Platforms { get; set; } = [];

    public Dictionary<string, ConfigurationBase> Configurations { get; set; } = [];

    public GameSettings? Defaults { get; set; }

    public Dictionary<string, GameSettings> Games { get; set; } = [];

    public HooksSettings Hooks { get; set; } = new();

    public GladSettings? Glad { get; set; }

    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters =
        {
            new JsonStringEnumConverter<ConfigurationBase>(JsonNamingPolicy.CamelCase, allowIntegerValues: false),
            new JsonStringEnumConverter<GladProfile>(JsonNamingPolicy.CamelCase, allowIntegerValues: false),
        },
        TypeInfoResolver = JsonContext.Default,
    };

    public static Manifest Load(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize(stream, TypeInfo<Manifest>(Options))!;
        }
        catch (JsonException e)
        {
            throw new JohnException($"{path} has a problem! {e.Message}");
        }
    }

    public static JsonTypeInfo<T> TypeInfo<T>(JsonSerializerOptions options)
    {
        return (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
    }
}
