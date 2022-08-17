using System.Text.RegularExpressions;

namespace CrossLaunch.Ubiquitous.Formats;

public record UnityProjectVersionFile(UnityVersion Version)
{
    private static readonly Regex s_projectVersionRegex = new(@"m_EditorVersionWithRevision:\s*(?<EditorVersion>\S+)\s*\((?<Revision>\S+)\)");

    public static async Task<UnityProjectVersionFile> LoadAsync(TextReader reader)
    {
        return s_projectVersionRegex.Match(await reader.ReadToEndAsync()) is { Success: true } match
            ? new UnityProjectVersionFile(new UnityVersion(match.Groups["EditorVersion"].Value, match.Groups["Revision"].Value))
            : throw new InvalidDataException("Missing m_EditorVersionWithRevision element");
    }
}

public readonly record struct UnityVersion(string EditorVersion, string Revision)
{
    public static bool TryParseFromCombined(string combined, out UnityVersion parsed)
    {
        int index = combined.LastIndexOf('/');
        if (index == -1)
        {
            parsed = default;
            return false;
        }
        parsed = new UnityVersion(combined[..index], combined[(index + 1)..]);
        return true;
    }

    public string Combined => $"{EditorVersion}/{Revision}";
}
