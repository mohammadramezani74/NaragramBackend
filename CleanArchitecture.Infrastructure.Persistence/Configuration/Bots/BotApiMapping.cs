using CleanArchitecture.Domain.Entities.Bots;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.Bots
{
    internal sealed class BotApiKeyMapping : IEntityTypeConfiguration<BotApiKey>
    {
        public void Configure(EntityTypeBuilder<BotApiKey> builder)
        {
            builder.ToTable("BotApiKeys");

            builder.HasKey(k => k.Id);
            builder.Property(k => k.Id).ValueGeneratedNever();

            builder.Property(k => k.Name).IsRequired().HasMaxLength(BotApiKey.MaxNameLength);
            builder.Property(k => k.Prefix).IsRequired().HasMaxLength(BotApiKey.PrefixLength);
            builder.Property(k => k.KeyHash).IsRequired().HasMaxLength(BotApiKey.HashLength).IsUnicode(false);

            // هر درخواست بیرونی کلید را با هشش پیدا می‌کند.
            builder.HasIndex(k => k.KeyHash).IsUnique().HasDatabaseName("UX_BotApiKeys_KeyHash");
            builder.HasIndex(k => k.BotId);

            builder.HasOne<Bot>()
                .WithMany()
                .HasForeignKey(k => k.BotId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Ignore(k => k.IsActive);
            builder.Ignore(k => k.CreatedByUser);
            builder.Ignore(k => k.ModifiedBy);
        }
    }

    internal sealed class BotApiSendMapping : IEntityTypeConfiguration<BotApiSend>
    {
        public void Configure(EntityTypeBuilder<BotApiSend> builder)
        {
            builder.ToTable("BotApiSends");

            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).ValueGeneratedNever();

            builder.Property(s => s.ExternalId).HasMaxLength(BotApiSend.MaxExternalIdLength);
            builder.Property(s => s.Reference).HasMaxLength(BotApiSend.MaxExternalIdLength);
            builder.Property(s => s.Phone).IsRequired().HasMaxLength(20);
            builder.Property(s => s.Status).IsRequired().HasMaxLength(16);
            builder.Property(s => s.Error).HasMaxLength(50);
            builder.Property(s => s.Preview).HasMaxLength(100);
            builder.Property(s => s.ClientIp).HasMaxLength(64);

            // ارسال تکراری با همان ExternalId (حتی دو درخواست هم‌زمان) رد می‌شود.
            builder.HasIndex(s => new { s.BotId, s.ExternalId })
                .IsUnique()
                .HasFilter("[ExternalId] IS NOT NULL")
                .HasDatabaseName("UX_BotApiSends_Bot_ExternalId");
            builder.HasIndex(s => new { s.BotId, s.At });

            builder.HasOne<Bot>()
                .WithMany()
                .HasForeignKey(s => s.BotId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<BotApiKey>()
                .WithMany()
                .HasForeignKey(s => s.ApiKeyId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
