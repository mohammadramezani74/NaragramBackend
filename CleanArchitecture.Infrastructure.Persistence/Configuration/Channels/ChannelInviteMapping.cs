using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

using CleanArchitecture.Domain.Entities.ChannelsAgg;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.Channels
{
    public sealed class ChannelInviteMapping : IEntityTypeConfiguration<ChannelInvite>
    {
        public void Configure(EntityTypeBuilder<ChannelInvite> builder)
        {
            builder.HasKey(ci => ci.Id);

            builder.Property(ci => ci.Token)
                .IsRequired()
                .HasMaxLength(64);

            builder.HasOne(ci => ci.Channel)
                .WithMany(c => c.Invites)
                .HasForeignKey(ci => ci.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(ci => ci.Token).IsUnique();
        }
    }
}
