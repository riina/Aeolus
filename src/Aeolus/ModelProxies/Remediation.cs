namespace Aeolus.ModelProxies;

public class Remediation
{
    public string ActionShortName { get; set; }

    public string ActionDescription { get; set; }

    public Func<Task> Callback { get; set; }
}
