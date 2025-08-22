using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

using CleanArchitecture.Domain.Entities.ChannelsAgg;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.Channels
{
    public sealed class ChannelMapping : IEntityTypeConfiguration<Channel>
    {
        public void Configure(EntityTypeBuilder<Channel> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Title)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(c => c.Description)
                .HasMaxLength(1000);

            builder.Property(c => c.UserName)
                .HasMaxLength(100);

            builder.HasIndex(c => c.UserName)
                .IsUnique();

            builder.Property(c => c.IsPublic)
                .IsRequired();


            builder.HasMany(c => c.Members)
                .WithOne(m => m.Channel)
                .HasForeignKey(m => m.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Admins)
                .WithOne(a => a.Channel)
                .HasForeignKey(a => a.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Invites)
                .WithOne(i => i.Channel)
                .HasForeignKey(i => i.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.ModifiedBy).WithMany().HasForeignKey(x => x.ModifiedById).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
