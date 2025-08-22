using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

using CleanArchitecture.Domain.Entities.ChannelsAgg;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.Channels
{
    public sealed class ChannelMessageSeenMapping : IEntityTypeConfiguration<ChannelMessageSeen>
    {
        public void Configure(EntityTypeBuilder<ChannelMessageSeen> builder)
        {
            builder.HasKey(s => s.Id);

            builder.HasIndex(s => new { s.MessageId, s.UserId }).IsUnique();

            builder.HasOne(s => s.Message)
                .WithMany()
                .HasForeignKey(s => s.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.ModifiedBy).WithMany().HasForeignKey(x => x.ModifiedById).OnDelete(DeleteBehavior.Restrict);
        }

    }
}
