namespace CrossLaunch.Models;

public class BaseProjectModel : BaseRecordModel
{
    public virtual string FullPath { get; set; } = null!;

    public virtual DateTime ModificationTime { get; set; }
}
