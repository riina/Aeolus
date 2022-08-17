using System.Diagnostics.CodeAnalysis;
using CrossLaunch.Models;

namespace CrossLaunch.Ubiquitous.Formats;

public class VSSolutionFile
{
    public readonly string MinimumVisualStudioVersion;
    public readonly string VisualStudioVersion;
    public IReadOnlyList<ProjectDefinition> ProjectDefinitions;

    public VSSolutionFile(string minimumVisualStudioVersion, string visualStudioVersion, IReadOnlyList<ProjectDefinition> projectDefinitions)
    {
        MinimumVisualStudioVersion = minimumVisualStudioVersion;
        VisualStudioVersion = visualStudioVersion;
        ProjectDefinitions = projectDefinitions;
    }

    public static VSSolutionFile Load(TextReader reader)
    {
        string? readLine;
        while ((readLine = reader.ReadLine()) != null)
            if (readLine.StartsWith("Microsoft Visual Studio Solution File"))
                break;
        if (readLine == null) throw new InvalidDataException("Missing header");
        ReadOnlySpan<char> visualStudioVersion = ReadOnlySpan<char>.Empty;
        ReadOnlySpan<char> minimumVisualStudioVersion = ReadOnlySpan<char>.Empty;
        while ((readLine = reader.ReadLine()) != null)
        {
            ReadOnlySpan<char> line = readLine;
            var l = line.TrimStart();
            if (l.StartsWith("#")) continue;
            if (TryGetKeyValue(l, "VisualStudioVersion", out var tmpVisualStudioVersion)) visualStudioVersion = tmpVisualStudioVersion;
            if (TryGetKeyValue(l, "MinimumVisualStudioVersion", out var tmpMinimumVisualStudioVersion)) minimumVisualStudioVersion = tmpMinimumVisualStudioVersion;
            if (visualStudioVersion.Length != 0 && minimumVisualStudioVersion.Length != 0) break;
        }
        if (visualStudioVersion.Length == 0 || minimumVisualStudioVersion.Length == 0) throw new InvalidDataException("Missing version info");
        ParseState state = new(new List<ProjectDefinition>());
        ProcessSection(ReadOnlySpan<char>.Empty, ref state, reader);
        return new VSSolutionFile(new string(minimumVisualStudioVersion), new string(visualStudioVersion), state.ProjectDefinitions);
    }

    private static void ProcessSection(ReadOnlySpan<char> section, ref ParseState state, TextReader reader)
    {
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            ReadOnlySpan<char> l = line.TrimStart();
            if (section.Length != 0 && l.StartsWith("End") && l[3..].TrimEnd().SequenceEqual(section))
            {
                return;
            }
            ReadOnlySpan<char> key;
            ReadOnlySpan<char> name = ReadOnlySpan<char>.Empty;
            ReadOnlySpan<char> value;
            if (TryGetKeyValue(l, out var k1, out var v1))
            {
                key = k1;
                value = v1;
                if (TryGetNamedKey(key, out var k2, out var n1))
                {
                    key = k2;
                    name = n1;
                }
            }
            else
            {
                key = l;
                value = ReadOnlySpan<char>.Empty;
            }
            if (key.SequenceEqual("Project"))
            {
                ProcessSection("Project", ref state, reader);
                var nameEntries = ParseEntries(name);
                var valueEntries = ParseEntries(value);
                if (nameEntries.Count != 1) throw new InvalidDataException("Unexpected number of entries for Project element kind");
                if (!Guid.TryParseExact(nameEntries[0], "B", out var nameGuid)) throw new InvalidDataException("Invalid GUID for Project element kind");
                if (valueEntries.Count != 3) throw new InvalidDataException("Unexpected number of entries for Project element data");
                if (!Guid.TryParseExact(valueEntries[2], "B", out var projectGuid)) throw new InvalidDataException("Invalid GUID for Project");
                state.ProjectDefinitions.Add(new ProjectDefinition(nameGuid, valueEntries[0], valueEntries[1], projectGuid));
            }
            else if (key.SequenceEqual("Global"))
            {
                ProcessSection("Global", ref state, reader);
            }
            else if (key.SequenceEqual("GlobalSection"))
            {
                ProcessSection("GlobalSection", ref state, reader);
            }
        }
        if (section.Length != 0) throw new InvalidDataException("Unexpected EOF");
    }

    private struct ParseState
    {
        public List<ProjectDefinition> ProjectDefinitions;

        public ParseState(List<ProjectDefinition> projectDefinitions)
        {
            ProjectDefinitions = projectDefinitions;
        }
    }

    private static bool TryGetKeyValue(ReadOnlySpan<char> source, ReadOnlySpan<char> pattern, out ReadOnlySpan<char> result)
    {
        source = source.TrimStart();
        if (source.StartsWith(pattern))
        {
            source = source[pattern.Length..].TrimStart();
            if (source.StartsWith("="))
            {
                result = source[1..].Trim();
                return true;
            }
        }
        result = ReadOnlySpan<char>.Empty;
        return false;
    }

    private static bool TryGetKeyValue(ReadOnlySpan<char> source, out ReadOnlySpan<char> key, out ReadOnlySpan<char> value)
    {
        int index = source.IndexOf('=');
        if (index != -1)
        {
            key = source[..index].Trim();
            value = source[(index + 1)..].Trim();
            return true;
        }
        key = ReadOnlySpan<char>.Empty;
        value = ReadOnlySpan<char>.Empty;
        return false;
    }

    private static bool TryGetNamedKey(ReadOnlySpan<char> key, out ReadOnlySpan<char> keyType, out ReadOnlySpan<char> keyName)
    {
        int index = key.IndexOf('(');
        if (index != -1 && key[^1] == ')')
        {
            keyType = key[..index];
            keyName = key[(index + 1)..^1];
            return true;
        }
        keyType = ReadOnlySpan<char>.Empty;
        keyName = ReadOnlySpan<char>.Empty;
        return false;
    }

    public static bool TryGetMinimumVisualStudio(BaseProjectModel project, [NotNullWhen(true)] out string? result)
    {
        ReadOnlySpan<char> slice = project.Framework;
        int index = slice.IndexOf('/');
        if (index != -1)
        {
            slice = slice[..index];
            int index2 = slice.IndexOf('.');
            if (index2 != -1)
            {
                result = new string(slice[..index2]);
                return true;
            }
        }
        result = null;
        return false;
    }

    private static List<string> ParseEntries(ReadOnlySpan<char> buf)
    {
        var res = new List<string>();
        buf = buf.Trim();
        while (!buf.IsEmpty)
        {
            if (buf[0] != '"') throw new InvalidDataException("Expected '\"'");
            buf = buf[1..];
            int index2 = buf.IndexOf('"');
            if (index2 == -1) throw new InvalidDataException("Expected '\"'");
            ReadOnlySpan<char> sub = buf[..index2];
            buf = buf[(index2 + 1)..].TrimStart();
            if (buf.StartsWith(",")) buf = buf[1..].TrimStart();
            else if (!buf.IsEmpty) throw new InvalidDataException("Expected ''");
            res.Add(new string(sub));
        }
        return res;
    }

    public record ProjectDefinition(Guid Kind, string Directory, string File, Guid Guid);
}
