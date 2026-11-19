using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace John;

[JsonSerializable(typeof(Manifest))]
[JsonSerializable(typeof(CMakeConfigurator.Configured))]
[JsonSerializable(typeof(Stager.Record))]
[JsonSerializable(typeof(string))]
internal sealed partial class JsonContext : JsonSerializerContext;

internal static class JsonHelper
{
    public static T? Read<T>(string path, JsonTypeInfo<T> type) where T : class
    {
        if (!File.Exists(path))
        {
            return null;
        }

        return JsonSerializer.Deserialize(File.ReadAllText(path), type);
    }

    public static bool Write<T>(string path, T value, JsonTypeInfo<T> type)
    {
        return FileHelper.WriteIfChanged(path, JsonSerializer.Serialize(value, type));
    }
}
