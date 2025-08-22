using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

using CleanArchitecture.Domain.Entities.ChannelsAgg;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.Channels
{
    public sealed class ChannelAdminMapping : IEntityTypeConfiguration<ChannelAdmin>
    {
        public void Configure(EntityTypeBuilder<ChannelAdmin> builder)
        {
            builder.HasKey(m => m.Id);

            builder.HasIndex(m => new { m.ChannelId, m.UserId }).IsUnique();


            builder.HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.ModifiedBy).WithMany().HasForeignKey(x => x.ModifiedById).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
