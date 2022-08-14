using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrossLaunch.Models;

public class RecentProjectModel : BaseProjectModel
{
}

public class RecentProjectModelConfiguration : IEntityTypeConfiguration<RecentProjectModel>
{
    public void Configure(EntityTypeBuilder<RecentProjectModel> builder)
    {
        builder.HasKey(v => v.FullPath);
    }
}
