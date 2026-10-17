using CleanArchitecture.Domain.Entities.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.Admin
{
    internal sealed class AppSettingMapping : IEntityTypeConfiguration<AppSetting>
    {
        public void Configure(EntityTypeBuilder<AppSetting> builder)
        {
            builder.ToTable("AppSettings");
            builder.HasKey(s => s.Key);
            builder.Property(s => s.Key).HasMaxLength(64);
            builder.Property(s => s.ValueJson).IsRequired();
            builder.Property(s => s.UpdatedByName).IsRequired().HasMaxLength(200);
        }
    }
}
