using System.Collections.Immutable;
using System.Text.Json;

namespace CrossLaunch;

public partial class CLConfiguration
{
    public IReadOnlyList<IProjectEvaluator> Evaluators { get; set; } = Array.Empty<IProjectEvaluator>();

    public int MaxRecentProjects { get; set; } = 10;

    public int MaxDepth { get; set; } = 1;

    public int MaxRecentProjectsWithOverride => TryGetInt64Option("recent.max", out long? value) ? (int)value : MaxRecentProjects;

    public int MaxDepthWithOverride => TryGetInt64Option("scan.maxdepth", out long? value) ? (int)value : MaxDepth;

    public IReadOnlyDictionary<string, JsonElement> Options { get; set; } = ImmutableDictionary<string, JsonElement>.Empty;

    public static IReadOnlyDictionary<string, JsonElement> LoadOptions(Stream stream)
    {
        IReadOnlyDictionary<string, JsonElement>? dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(stream, s_serializerOptions);
        return dict != null ? new Dictionary<string, JsonElement>(dict, StringComparer.InvariantCultureIgnoreCase) : ImmutableDictionary<string, JsonElement>.Empty;
    }

    public static async Task<IReadOnlyDictionary<string, JsonElement>> LoadOptionsAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, JsonElement>? dict = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(stream, s_serializerOptions, cancellationToken);
        return dict != null ? new Dictionary<string, JsonElement>(dict, StringComparer.InvariantCultureIgnoreCase) : ImmutableDictionary<string, JsonElement>.Empty;
    }

    public static void SerializeOptions(IReadOnlyDictionary<string, JsonElement> options, Stream stream)
    {
        JsonSerializer.Serialize(stream, options, s_serializerOptions);
    }

    public static async Task SerializeOptionsAsync(IReadOnlyDictionary<string, JsonElement> options, Stream stream, CancellationToken cancellationToken = default)
    {
        await JsonSerializer.SerializeAsync(stream, options, s_serializerOptions, cancellationToken);
    }

    private static readonly JsonSerializerOptions s_serializerOptions = new() { ReadCommentHandling = JsonCommentHandling.Skip, PropertyNameCaseInsensitive = true, WriteIndented = true };
}
