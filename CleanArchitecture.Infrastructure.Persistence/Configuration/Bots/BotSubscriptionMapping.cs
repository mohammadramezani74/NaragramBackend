using CleanArchitecture.Domain.Entities.Bots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.Bots
{
    internal sealed class BotSubscriptionMapping : IEntityTypeConfiguration<BotSubscription>
    {
        public void Configure(EntityTypeBuilder<BotSubscription> builder)
        {
            builder.ToTable("BotSubscriptions");

            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).ValueGeneratedNever();
            builder.Property(s => s.Topic).IsRequired().HasMaxLength(BotSubscription.MaxTopicLength);
            builder.Property(s => s.City).HasMaxLength(BotSubscription.MaxCityLength);

            builder.HasIndex(s => new { s.BotId, s.UserId, s.Topic }).IsUnique().HasDatabaseName("UX_BotSubscriptions_Bot_User_Topic");
            builder.HasIndex(s => new { s.BotId, s.Topic, s.LastSentOn });

            builder.HasOne<Bot>()
                .WithMany()
                .HasForeignKey(s => s.BotId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
