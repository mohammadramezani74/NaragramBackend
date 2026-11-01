using CleanArchitecture.Domain.Entities.Bots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.Bots
{
    internal sealed class BotFlowRunMapping : IEntityTypeConfiguration<BotFlowRun>
    {
        public void Configure(EntityTypeBuilder<BotFlowRun> builder)
        {
            builder.ToTable("BotFlowRuns");

            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).ValueGeneratedNever();
            builder.Property(r => r.FlowKey).IsRequired().HasMaxLength(BotFlowRun.MaxFlowKeyLength);
            builder.Property(r => r.Outcome).IsRequired().HasMaxLength(BotFlowRun.MaxOutcomeLength);

            // گزارش بر اساس بازه‌ی زمانی، و پیدا کردن جریان باز یک کاربر در یک ربات
            builder.HasIndex(r => r.StartedAt);
            builder.HasIndex(r => new { r.BotId, r.UserId, r.StartedAt });

            builder.HasOne<Bot>()
                .WithMany()
                .HasForeignKey(r => r.BotId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
