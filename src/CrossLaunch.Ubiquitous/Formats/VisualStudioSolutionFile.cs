using System.Diagnostics.CodeAnalysis;
using CrossLaunch.Models;

namespace CrossLaunch.Ubiquitous.Formats;

public record VisualStudioSolutionFile(string MinimumVisualStudioVersion, string VisualStudioVersion, IReadOnlyList<VisualStudioSolutionFileProject> ProjectDefinitions)
{
    public static async Task<VisualStudioSolutionFile> LoadAsync(TextReader reader)
    {
        string? readLine;
        while ((readLine = await reader.ReadLineAsync()) != null)
            if (readLine.StartsWith("Microsoft Visual Studio Solution File"))
                break;
        if (readLine == null) throw new InvalidDataException("Missing header");
        ReadOnlyMemory<char> visualStudioVersion = ReadOnlyMemory<char>.Empty;
        ReadOnlyMemory<char> minimumVisualStudioVersion = ReadOnlyMemory<char>.Empty;
        while ((readLine = await reader.ReadLineAsync()) != null)
        {
            ReadOnlyMemory<char> line = readLine.AsMemory();
            var l = line.TrimStart();
            if (l.Span.StartsWith("#")) continue;
            if (TryGetKeyValue(l, "VisualStudioVersion".AsMemory(), out var tmpVisualStudioVersion)) visualStudioVersion = tmpVisualStudioVersion;
            if (TryGetKeyValue(l, "MinimumVisualStudioVersion".AsMemory(), out var tmpMinimumVisualStudioVersion)) minimumVisualStudioVersion = tmpMinimumVisualStudioVersion;
            if (visualStudioVersion.Length != 0 && minimumVisualStudioVersion.Length != 0) break;
        }
        if (visualStudioVersion.Length == 0 || minimumVisualStudioVersion.Length == 0) throw new InvalidDataException("Missing version info");
        ParseState state = new(new List<VisualStudioSolutionFileProject>());
        await ProcessSection(ReadOnlyMemory<char>.Empty, state, reader);
        return new VisualStudioSolutionFile(new string(minimumVisualStudioVersion.Span), new string(visualStudioVersion.Span), state.ProjectDefinitions);
    }

    private static async Task ProcessSection(ReadOnlyMemory<char> section, ParseState state, TextReader reader)
    {
        while (await reader.ReadLineAsync() is { } line)
        {
            ReadOnlyMemory<char> l = line.TrimStart().AsMemory();
            if (section.Length != 0 && l.Span.StartsWith("End") && l[3..].TrimEnd().Span.SequenceEqual(section.Span))
            {
                return;
            }
            ReadOnlyMemory<char> key;
            ReadOnlyMemory<char> name = ReadOnlyMemory<char>.Empty;
            ReadOnlyMemory<char> value;
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
                value = ReadOnlyMemory<char>.Empty;
            }
            if (key.Span.SequenceEqual("Project"))
            {
                await ProcessSection("Project".AsMemory(), state, reader);
                var nameEntries = ParseEntries(name);
                var valueEntries = ParseEntries(value);
                if (nameEntries.Count != 1) throw new InvalidDataException("Unexpected number of entries for Project element kind");
                if (!Guid.TryParseExact(nameEntries[0], "B", out var nameGuid)) throw new InvalidDataException("Invalid GUID for Project element kind");
                if (valueEntries.Count != 3) throw new InvalidDataException("Unexpected number of entries for Project element data");
                if (!Guid.TryParseExact(valueEntries[2], "B", out var projectGuid)) throw new InvalidDataException("Invalid GUID for Project");
                state.ProjectDefinitions.Add(new VisualStudioSolutionFileProject(nameGuid, valueEntries[0], valueEntries[1], projectGuid));
            }
            else if (key.Span.SequenceEqual("Global"))
            {
                await ProcessSection("Global".AsMemory(), state, reader);
            }
            else if (key.Span.SequenceEqual("GlobalSection"))
            {
                await ProcessSection("GlobalSection".AsMemory(), state, reader);
            }
        }
        if (section.Length != 0) throw new InvalidDataException("Unexpected EOF");
    }

    private class ParseState
    {
        public List<VisualStudioSolutionFileProject> ProjectDefinitions;

        public ParseState(List<VisualStudioSolutionFileProject> projectDefinitions)
        {
            ProjectDefinitions = projectDefinitions;
        }
    }

    private static bool TryGetKeyValue(ReadOnlyMemory<char> source, ReadOnlyMemory<char> pattern, out ReadOnlyMemory<char> result)
    {
        source = source.TrimStart();
        if (source.Span.StartsWith(pattern.Span))
        {
            source = source[pattern.Length..].TrimStart();
            if (source.Span.StartsWith("="))
            {
                result = source[1..].Trim();
                return true;
            }
        }
        result = ReadOnlyMemory<char>.Empty;
        return false;
    }

    private static bool TryGetKeyValue(ReadOnlyMemory<char> source, out ReadOnlyMemory<char> key, out ReadOnlyMemory<char> value)
    {
        int index = source.Span.IndexOf('=');
        if (index != -1)
        {
            key = source[..index].Trim();
            value = source[(index + 1)..].Trim();
            return true;
        }
        key = ReadOnlyMemory<char>.Empty;
        value = ReadOnlyMemory<char>.Empty;
        return false;
    }

    private static bool TryGetNamedKey(ReadOnlyMemory<char> key, out ReadOnlyMemory<char> keyType, out ReadOnlyMemory<char> keyName)
    {
        int index = key.Span.IndexOf('(');
        if (index != -1 && key.Span[^1] == ')')
        {
            keyType = key[..index];
            keyName = key[(index + 1)..^1];
            return true;
        }
        keyType = ReadOnlyMemory<char>.Empty;
        keyName = ReadOnlyMemory<char>.Empty;
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

    private static List<string> ParseEntries(ReadOnlyMemory<char> src)
    {
        var buf = src.Span;
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
}

public record VisualStudioSolutionFileProject(Guid Kind, string Directory, string File, Guid Guid);
