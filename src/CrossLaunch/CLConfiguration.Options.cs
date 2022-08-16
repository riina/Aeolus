using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace CrossLaunch;

/*
 * Copied from Art ArtifactTool option helpers
 */
public partial class CLConfiguration
{
    private static readonly HashSet<string> s_yesLower = new() { "y", "yes", "" };

    #region Base

    /// <summary>
    /// Attempts to get option or throw exception if not found or if null.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="optKey">Key to search.</param>
    /// <returns>Value, if located and nonnull.</returns>
    /// <exception cref="CLConfigurationOptionNotFoundException">Thrown when option is not found.</exception>
    /// <exception cref="JsonException">Thrown when conversion failed.</exception>
    /// <exception cref="NotSupportedException">Thrown when type not supported.</exception>
    /// <exception cref="NullJsonDataException">Thrown for null JSON.</exception>
    public T GetOption<T>(string optKey)
    {
        if (!Options.TryGetValue(optKey, out JsonElement vv)) throw new CLConfigurationOptionNotFoundException(optKey);
        return vv.Deserialize<T>(s_serializerOptions) ?? throw new NullJsonDataException();
    }

    /// <summary>
    /// Attempts to get option or throw exception if not found or if null.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="optKey">Key to search.</param>
    /// <param name="value">Value to set.</param>
    /// <param name="throwIfIncorrectType">If true, throw a <see cref="JsonException"/> if type is wrong.</param>
    /// <exception cref="JsonException">Thrown when conversion failed.</exception>
    /// <exception cref="NotSupportedException">Thrown when type not supported.</exception>
    /// <exception cref="NullJsonDataException">Thrown for null JSON.</exception>
    public void GetOption<T>(string optKey, ref T value, bool throwIfIncorrectType = false)
    {
        if (!Options.TryGetValue(optKey, out JsonElement vv)) return;
        if (vv.ValueKind == JsonValueKind.Null) throw new NullJsonDataException();
        try
        {
            value = vv.Deserialize<T>(s_serializerOptions) ?? throw new NullJsonDataException();
        }
        catch (JsonException)
        {
            if (throwIfIncorrectType) throw;
        }
    }

    /// <summary>
    /// Attempts to get option.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="optKey">Key to search.</param>
    /// <param name="value">Value, if located and nonnull.</param>
    /// <param name="throwIfIncorrectType">If true, throw a <see cref="JsonException"/> if type is wrong.</param>
    /// <returns>True if value is located and of the right type.</returns>
    public bool TryGetOption<T>(string optKey, [NotNullWhen(true)] out T? value, bool throwIfIncorrectType = false)
    {
        if (Options.TryGetValue(optKey, out JsonElement vv))
        {
            try
            {
                value = vv.Deserialize<T>(s_serializerOptions);
                return value != null;
            }
            catch (JsonException)
            {
                if (throwIfIncorrectType) throw;
            }
        }
        value = default;
        return false;
    }

    #endregion

    #region String

    /// <summary>
    /// Attempts to get option or throw exception if not found.
    /// </summary>
    /// <param name="optKey">Key to search.</param>
    /// <returns>Value, if located.</returns>
    /// <exception cref="CLConfigurationOptionNotFoundException">Thrown when option is not found.</exception>
    public string GetStringOption(string optKey)
    {
        if (!Options.TryGetValue(optKey, out JsonElement vv)) throw new CLConfigurationOptionNotFoundException(optKey);
        return vv.ToString();
    }

    /// <summary>
    /// Attempts to get option.
    /// </summary>
    /// <param name="optKey">Key to search.</param>
    /// <param name="value">Value to set.</param>
    /// <exception cref="NullJsonDataException">Thrown for null JSON.</exception>
    public void GetStringOption(string optKey, ref string value)
    {
        if (!Options.TryGetValue(optKey, out JsonElement vv)) return;
        if (vv.ValueKind == JsonValueKind.Null) throw new NullJsonDataException();
        value = vv.ToString();
    }

    /// <summary>
    /// Attempts to get option.
    /// </summary>
    /// <param name="optKey">Key to search.</param>
    /// <param name="value">Value, if located and nonnull.</param>
    /// <returns>True if value is located and of the right type.</returns>
    public bool TryGetStringOption(string optKey, [NotNullWhen(true)] out string? value)
    {
        if (Options.TryGetValue(optKey, out JsonElement vv))
        {
            value = vv.ToString();
            return true;
        }
        value = default;
        return false;
    }

    #endregion

    #region Flag

    /// <summary>
    /// Checks if a flag is true.
    /// </summary>
    /// <param name="optKey">Key to search.</param>
    /// <param name="throwIfIncorrectType">If true, throw a <see cref="JsonException"/> or <see cref="NotSupportedException"/> if type is wrong.</param>
    /// <returns>True if flag is set to true.</returns>
    /// <exception cref="JsonException">Thrown when conversion failed.</exception>
    /// <exception cref="NotSupportedException">Thrown when type not supported.</exception>
    public bool GetFlag(string optKey, bool throwIfIncorrectType = false)
    {
        return TryGetOption(optKey, out bool? value) && value.Value
               || TryGetOption(optKey, out string? valueStr, throwIfIncorrectType) && s_yesLower.Contains(valueStr.ToLowerInvariant());
    }

    /// <summary>
    /// Modifies a ref bool if a flag option is present.
    /// </summary>
    /// <param name="optKey">Key to search.</param>
    /// <param name="flag">Value to set.</param>
    /// <param name="throwIfIncorrectType">If true, throw a <see cref="JsonException"/> or <see cref="NotSupportedException"/> if type is wrong.</param>
    /// <exception cref="JsonException">Thrown when conversion failed.</exception>
    /// <exception cref="NotSupportedException">Thrown when type not supported.</exception>
    public void GetFlag(string optKey, ref bool flag, bool throwIfIncorrectType = false)
    {
        if (TryGetOption(optKey, out bool? value)) flag = value.Value;
        if (TryGetOption(optKey, out string? valueStr, throwIfIncorrectType)) flag = s_yesLower.Contains(valueStr.ToLowerInvariant());
    }

    /// <summary>
    /// Checks if a flag is true.
    /// </summary>
    /// <param name="optKey">Key to search.</param>
    /// <param name="flag">Value, if found and nonnull.</param>
    /// <param name="throwIfIncorrectType">If true, throw a <see cref="JsonException"/> or <see cref="NotSupportedException"/> if type is wrong.</param>
    /// <returns>True if value is located.</returns>
    /// <exception cref="JsonException">Thrown when conversion failed.</exception>
    /// <exception cref="NotSupportedException">Thrown when type not supported.</exception>
    public bool TryGetFlag(string optKey, out bool flag, bool throwIfIncorrectType = false)
    {
        if (TryGetOption(optKey, out bool? value))
        {
            flag = value.Value;
            return true;
        }
        if (TryGetOption(optKey, out string? valueStr, throwIfIncorrectType))
        {
            flag = s_yesLower.Contains(valueStr.ToLowerInvariant());
            return true;
        }
        flag = false;
        return false;
    }

    #endregion

    #region Int

    /// <summary>
    /// Gets an Int64 option from a string or literal value.
    /// </summary>
    /// <param name="optKey">Key to search.</param>
    /// <param name="throwIfIncorrectType">If true, throw a <see cref="JsonException"/> or <see cref="NotSupportedException"/> if type is wrong.</param>
    /// <returns>Value.</returns>
    /// <exception cref="CLConfigurationOptionNotFoundException">Thrown when option is not found.</exception>
    /// <exception cref="JsonException">Thrown when conversion failed.</exception>
    /// <exception cref="NotSupportedException">Thrown when type not supported.</exception>
    public long GetInt64Option(string optKey, bool throwIfIncorrectType = false)
    {
        if (!TryGetOption(optKey, out long valueL, throwIfIncorrectType)) throw new CLConfigurationOptionNotFoundException(optKey);
        return valueL;
    }

    /// <summary>
    /// Attempts to get an Int64 option from a string or literal value.
    /// </summary>
    /// <param name="optKey">Key to search.</param>
    /// <param name="value">Value to set.</param>
    /// <param name="throwIfIncorrectType">If true, throw a <see cref="JsonException"/> or <see cref="NotSupportedException"/> if type is wrong.</param>
    /// <returns>True if found.</returns>
    /// <exception cref="JsonException">Thrown when conversion failed.</exception>
    /// <exception cref="NotSupportedException">Thrown when type not supported.</exception>
    public void GetInt64Option(string optKey, ref long value, bool throwIfIncorrectType = false)
    {
        if (TryGetOption(optKey, out long valueL)) value = valueL;
        if (TryGetOption(optKey, out string? valueStr, throwIfIncorrectType) && long.TryParse(valueStr, out long valueParsed)) value = valueParsed;
    }

    /// <summary>
    /// Attempts to get a Int64 option from a string or literal value.
    /// </summary>
    /// <param name="optKey">Key to search.</param>
    /// <param name="value">Value, if located and nonnull.</param>
    /// <param name="throwIfIncorrectType">If true, throw a <see cref="JsonException"/> or <see cref="NotSupportedException"/> if type is wrong.</param>
    /// <returns>True if found.</returns>
    /// <exception cref="JsonException">Thrown when conversion failed.</exception>
    /// <exception cref="NotSupportedException">Thrown when type not supported.</exception>
    public bool TryGetInt64Option(string optKey, [NotNullWhen(true)] out long? value, bool throwIfIncorrectType = false)
    {
        if (TryGetOption(optKey, out value)) return true;
        if (TryGetOption(optKey, out string? valueStr, throwIfIncorrectType) && long.TryParse(valueStr, out long valueParsed))
        {
            value = valueParsed;
            return true;
        }
        return false;
    }

    #endregion

    #region UInt

    /// <summary>
    /// Gets a UInt64 option from a string or literal value.
    /// </summary>
    /// <param name="optKey">Key to search.</param>
    /// <param name="throwIfIncorrectType">If true, throw a <see cref="JsonException"/> or <see cref="NotSupportedException"/> if type is wrong.</param>
    /// <returns>Value.</returns>
    /// <exception cref="CLConfigurationOptionNotFoundException">Thrown when option is not found.</exception>
    /// <exception cref="JsonException">Thrown when conversion failed.</exception>
    /// <exception cref="NotSupportedException">Thrown when type not supported.</exception>
    public ulong GetUInt64Option(string optKey, bool throwIfIncorrectType = false)
    {
        if (!TryGetOption(optKey, out ulong valueL, throwIfIncorrectType)) throw new CLConfigurationOptionNotFoundException(optKey);
        return valueL;
    }

    /// <summary>
    /// Attempts to get a UInt64 option from a string or literal value.
    /// </summary>
    /// <param name="optKey">Key to search.</param>
    /// <param name="value">Value to set.</param>
    /// <param name="throwIfIncorrectType">If true, throw a <see cref="JsonException"/> or <see cref="NotSupportedException"/> if type is wrong.</param>
    /// <returns>True if found.</returns>
    /// <exception cref="JsonException">Thrown when conversion failed.</exception>
    /// <exception cref="NotSupportedException">Thrown when type not supported.</exception>
    public void GetUInt64Option(string optKey, ref ulong value, bool throwIfIncorrectType = false)
    {
        if (TryGetOption(optKey, out ulong valueL)) value = valueL;
        if (TryGetOption(optKey, out string? valueStr, throwIfIncorrectType) && ulong.TryParse(valueStr, out ulong valueParsed)) value = valueParsed;
    }

    /// <summary>
    /// Attempts to get an UInt64 option from a string or literal value.
    /// </summary>
    /// <param name="optKey">Key to search.</param>
    /// <param name="value">Value, if located and nonnull.</param>
    /// <param name="throwIfIncorrectType">If true, throw a <see cref="JsonException"/> or <see cref="NotSupportedException"/> if type is wrong.</param>
    /// <returns>True if found.</returns>
    /// <exception cref="JsonException">Thrown when conversion failed.</exception>
    /// <exception cref="NotSupportedException">Thrown when type not supported.</exception>
    public bool TryGetUInt64Option(string optKey, [NotNullWhen(true)] out ulong? value, bool throwIfIncorrectType = false)
    {
        if (TryGetOption(optKey, out value)) return true;
        if (TryGetOption(optKey, out string? valueStr, throwIfIncorrectType) && ulong.TryParse(valueStr, out ulong valueParsed))
        {
            value = valueParsed;
            return true;
        }
        return false;
    }

    #endregion
}

/// <summary>
/// Represents exception thrown when null JSON data is encountered.
/// </summary>
public class NullJsonDataException : Exception
{
    /// <summary>
    /// Creates a new instance of <see cref="NullJsonDataException"/>.
    /// </summary>
    public NullJsonDataException()
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="NullJsonDataException"/> with the specified message.
    /// </summary>
    /// <param name="message">Message.</param>
    public NullJsonDataException(string message) : base(message)
    {
    }
}

/// <summary>
/// Represents an exception thrown when an option needed by a <see cref="CLConfiguration"/> is not found.
/// </summary>
public class CLConfigurationOptionNotFoundException : Exception
{
    /// <summary>
    /// Missing options.
    /// </summary>
    public IReadOnlyList<string> Options { get; }

    private string? _message;

    /// <summary>
    /// Creates a new instance of <see cref="CLConfigurationOptionNotFoundException"/>.
    /// </summary>
    /// <param name="options">Missing options.</param>
    public CLConfigurationOptionNotFoundException(params string[] options)
    {
        Options = options;
    }

    private string GetMessageString()
    {
        StringBuilder sb = new();
        if (Options.Count == 1)
            sb.Append("Configuration was missing required option ");
        else
            sb.Append("Configuration was missing required options ");
        sb.AppendJoin(", ", Options);
        return sb.ToString();
    }

    /// <inheritdoc/>
    public override string Message => _message ??= GetMessageString();
}
