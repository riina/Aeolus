using System.Diagnostics.CodeAnalysis;

namespace CrossLaunch;

public record CLSemVer(long Major, long Minor, long Patch, string Extra) : IComparable<CLSemVer>
{
    public static bool TryParse(ReadOnlySpan<char> value, [NotNullWhen(true)] out CLSemVer? semVer)
    {
        int split = value.IndexOf('-');
        ReadOnlySpan<char> extra;
        if (split != -1)
        {
            extra = value[(split + 1)..];
            value = value[..split];
        }
        else
        {
            extra = ReadOnlySpan<char>.Empty;
        }
        int sep1 = value.IndexOf('.');
        if (sep1 == -1)
        {
            semVer = null;
            return false;
        }
        if (!long.TryParse(value[..sep1], out long major))
        {
            semVer = null;
            return false;
        }
        value = value[(sep1 + 1)..];
        int sep2 = value.IndexOf('.');
        if (sep2 == -1)
        {
            semVer = null;
            return false;
        }
        if (!long.TryParse(value[..sep2], out long minor))
        {
            semVer = null;
            return false;
        }
        if (!long.TryParse(value[(sep2 + 1)..], out long patch))
        {
            semVer = null;
            return false;
        }
        semVer = new CLSemVer(major, minor, patch, extra.IsEmpty ? "" : new string(extra));
        return true;
    }

    public static CLSemVer Parse(ReadOnlySpan<char> value)
    {
        int split = value.IndexOf('-');
        ReadOnlySpan<char> extra;
        if (split != -1)
        {
            extra = value[(split + 1)..];
            value = value[..split];
        }
        else
        {
            extra = ReadOnlySpan<char>.Empty;
        }
        int sep1 = value.IndexOf('.');
        if (sep1 == -1) throw new FormatException("Missing major-minor delimiter");
        long major = long.Parse(value[..sep1]);
        value = value[(sep1 + 1)..];
        int sep2 = value.IndexOf('.');
        if (sep2 == -1) throw new FormatException("Missing minor-patch delimiter");
        long minor = long.Parse(value[..sep2]);
        long patch = long.Parse(value[(sep2 + 1)..]);
        return new CLSemVer(major, minor, patch, extra.IsEmpty ? "" : new string(extra));
    }

    public int CompareTo(CLSemVer? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        int majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0) return majorComparison;
        int minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0) return minorComparison;
        int patchComparison = Patch.CompareTo(other.Patch);
        if (patchComparison != 0) return patchComparison;
        return string.Compare(Extra, other.Extra, StringComparison.Ordinal);
    }

    public static bool operator <(CLSemVer? left, CLSemVer? right) => Comparer<CLSemVer>.Default.Compare(left, right) < 0;

    public static bool operator >(CLSemVer? left, CLSemVer? right) => Comparer<CLSemVer>.Default.Compare(left, right) > 0;

    public static bool operator <=(CLSemVer? left, CLSemVer? right) => Comparer<CLSemVer>.Default.Compare(left, right) <= 0;

    public static bool operator >=(CLSemVer? left, CLSemVer? right) => Comparer<CLSemVer>.Default.Compare(left, right) >= 0;
}

public record CLSemVerTag(string Key, CLSemVer Version);
