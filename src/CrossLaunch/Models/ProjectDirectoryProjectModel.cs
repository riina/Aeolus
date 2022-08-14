using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrossLaunch.Models;

public class ProjectDirectoryProjectModel : BaseProjectModel
{
    public virtual ProjectDirectoryModel ProjectDirectory { get; set; } = null!;
}

public class ProjectDirectoryProjectModelConfiguration : IEntityTypeConfiguration<ProjectDirectoryProjectModel>
{
    public void Configure(EntityTypeBuilder<ProjectDirectoryProjectModel> builder)
    {
        builder.HasKey(v => v.FullPath);
    }
}
