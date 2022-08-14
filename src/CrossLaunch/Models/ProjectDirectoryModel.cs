using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrossLaunch.Models;

public class ProjectDirectoryModel : BaseRecordModel
{
    public virtual string FullPath { get; set; } = null!;

    public virtual HashSet<ProjectDirectoryProjectModel> Projects { get; set; } = null!;
}

public class ProjectDirectoryModelConfiguration : IEntityTypeConfiguration<ProjectDirectoryModel>
{
    public void Configure(EntityTypeBuilder<ProjectDirectoryModel> builder)
    {
        builder.HasKey(v => v.FullPath);
        builder.HasMany(v => v.Projects).WithOne(v => v.ProjectDirectory).OnDelete(DeleteBehavior.Cascade);
    }
}
