using System.Reflection;
using System.Text.RegularExpressions;

namespace CrossLaunch;

public static class TypeTool
{
    public static string CreateTypeString(Type type)
    {
        string assemblyName = type.Assembly.GetName().Name ?? throw new InvalidOperationException();
        string typeName = type.FullName ?? throw new InvalidOperationException();
        return $"{assemblyName}::{typeName}";
    }

    public static TypeString ParseTypeString(string tool)
    {
        if (tool == null) throw new ArgumentNullException(nameof(tool));
        if (s_toolRegex.Match(tool) is not { Success: true } match)
            throw new ArgumentException("Tool string is in invalid format, must be \"<assembly>::<toolType>\"", nameof(tool));
        return new TypeString(match.Groups[1].Value, match.Groups[2].Value);
    }

    private static readonly Regex s_toolRegex = new(@"^([\S\s]+)::([\S\s]+)$");

    public static List<Type> GetConcreteInterfaceImplementors<T>(Type anchor) => GetConcreteInterfaceImplementors<T>(anchor.Assembly);

    public static List<Type> GetConcreteInterfaceImplementors<T>(Assembly assembly)
    {
        var res = new List<Type>();
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract) continue;
            if (!type.GetInterfaces().Contains(typeof(T))) continue;
            res.Add(type);
        }
        return res;
    }

    public static List<T> CreateInstances<T>(IEnumerable<Type> types) where T : class
    {
        var res = new List<T>();
        foreach (var type in types)
        {
            try
            {
                if (Activator.CreateInstance(type) is T instance) res.Add(instance);
            }
            catch
            {
                // ignored
            }
        }
        return res;
    }
}

public readonly record struct TypeString(string Assembly, string Type)
{
    public Type Load()
    {
        var assembly = System.Reflection.Assembly.Load(Assembly);
        return assembly.GetType(Type) ?? throw new KeyNotFoundException($"Type with name \"{Type}\" not found");
    }
}
