namespace CrossLaunch.Models;

public class BaseProjectModel : BaseRecordModel
{
    public virtual string FullPath { get; set; } = null!;

    public virtual string ProjectEvaluatorType { get; set; } = null!;

    public virtual string Framework { get; set; } = null!;
}
