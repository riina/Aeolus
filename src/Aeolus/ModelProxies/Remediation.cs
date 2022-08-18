namespace Aeolus.ModelProxies;

public record Remediation(string ActionShortName, string ActionDescription, Func<Task> Callback);
