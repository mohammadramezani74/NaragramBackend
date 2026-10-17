using CleanArchitecture.Domain.Entities.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.Admin
{
    internal sealed class BroadcastMapping : IEntityTypeConfiguration<Broadcast>
    {
        public void Configure(EntityTypeBuilder<Broadcast> builder)
        {
            builder.ToTable("Broadcasts");

            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id).ValueGeneratedNever();

            builder.Property(b => b.Title).IsRequired().HasMaxLength(Broadcast.MaxTitleLength);
            builder.Property(b => b.Text).IsRequired().HasMaxLength(Broadcast.MaxTextLength);
            builder.Property(b => b.Kind).IsRequired().HasMaxLength(16);
            builder.Property(b => b.ButtonText).HasMaxLength(Broadcast.MaxButtonTextLength);
            builder.Property(b => b.ButtonUrl).HasMaxLength(Broadcast.MaxUrlLength);
            builder.Property(b => b.AudienceJson).IsRequired();
            builder.Property(b => b.AudienceLabel).IsRequired().HasMaxLength(300);
            builder.Property(b => b.Error).HasMaxLength(500);
            builder.Property(b => b.CreatedByName).IsRequired().HasMaxLength(200);

            builder.HasIndex(b => new { b.Status, b.ScheduledAt });

            builder.Ignore(b => b.CreatedByUser);
            builder.Ignore(b => b.ModifiedBy);
        }
    }

    internal sealed class BroadcastDeliveryMapping : IEntityTypeConfiguration<BroadcastDelivery>
    {
        public void Configure(EntityTypeBuilder<BroadcastDelivery> builder)
        {
            builder.ToTable("BroadcastDeliveries");

            builder.HasKey(d => d.Id);
            builder.Property(d => d.Id).ValueGeneratedNever();

            // هر نفر فقط یک بار؛ اگر ارسال وسط کار قطع شد، دوباره‌کاری نمی‌شود.
            builder.HasIndex(d => new { d.BroadcastId, d.UserId }).IsUnique().HasDatabaseName("UX_BroadcastDeliveries_Broadcast_User");
            builder.HasIndex(d => d.MessageId);

            builder.HasOne<Broadcast>()
                .WithMany()
                .HasForeignKey(d => d.BroadcastId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
