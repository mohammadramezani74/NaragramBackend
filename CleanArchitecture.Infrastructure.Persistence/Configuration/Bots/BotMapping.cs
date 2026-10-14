using CleanArchitecture.Domain.Entities.Bots;
using CleanArchitecture.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.Bots
{
    internal sealed class BotMapping : IEntityTypeConfiguration<Bot>
    {
        public void Configure(EntityTypeBuilder<Bot> builder)
        {
            builder.ToTable("Bots");

            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id).ValueGeneratedNever();

            builder.Property(b => b.Handle)
                .IsRequired()
                .HasMaxLength(Bot.MaxHandleLength);

            builder.Property(b => b.Description)
                .HasMaxLength(Bot.MaxDescriptionLength);

            builder.Property(b => b.AccessListJson).IsRequired();
            builder.Property(b => b.CommandsJson).IsRequired();

            builder.HasIndex(b => b.Handle).IsUnique().HasDatabaseName("UX_Bots_Handle");
            builder.HasIndex(b => b.UserId).IsUnique().HasDatabaseName("UX_Bots_UserId");

            builder.HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.CreatedByUser)
                .WithMany()
                .HasForeignKey(b => b.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.ModifiedBy)
                .WithMany()
                .HasForeignKey(b => b.ModifiedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    internal sealed class BotMenuNodeMapping : IEntityTypeConfiguration<BotMenuNode>
    {
        public void Configure(EntityTypeBuilder<BotMenuNode> builder)
        {
            builder.ToTable("BotMenuNodes");

            builder.HasKey(n => n.Id);
            builder.Property(n => n.Id).ValueGeneratedNever();

            builder.Property(n => n.Label)
                .IsRequired()
                .HasMaxLength(BotMenuNode.MaxLabelLength);

            builder.Property(n => n.Text).HasMaxLength(BotMenuNode.MaxTextLength);
            builder.Property(n => n.FlowKey).HasMaxLength(BotMenuNode.MaxFlowKeyLength);
            builder.Property(n => n.ParamsJson).HasMaxLength(BotMenuNode.MaxParamsLength);

            // درخت هر ربات همیشه کامل خوانده می‌شود.
            builder.HasIndex(n => new { n.BotId, n.ParentId, n.SortOrder })
                .HasDatabaseName("IX_BotMenuNodes_Bot_Parent_Order");

            builder.HasOne(n => n.Bot)
                .WithMany()
                .HasForeignKey(n => n.BotId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(n => n.CreatedByUser)
                .WithMany()
                .HasForeignKey(n => n.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(n => n.ModifiedBy)
                .WithMany()
                .HasForeignKey(n => n.ModifiedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    internal sealed class BotSessionMapping : IEntityTypeConfiguration<BotSession>
    {
        public void Configure(EntityTypeBuilder<BotSession> builder)
        {
            builder.ToTable("BotSessions");

            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).ValueGeneratedNever();

            builder.Property(s => s.FlowKey).HasMaxLength(BotMenuNode.MaxFlowKeyLength);
            builder.Property(s => s.FlowParamsJson).HasMaxLength(BotMenuNode.MaxParamsLength);

            builder.Ignore(s => s.IsStarted);
            builder.Ignore(s => s.HasFlow);

            builder.HasIndex(s => new { s.BotId, s.UserId })
                .IsUnique()
                .HasDatabaseName("UX_BotSessions_Bot_User");

            builder.HasOne(s => s.Bot)
                .WithMany()
                .HasForeignKey(s => s.BotId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.CreatedByUser)
                .WithMany()
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.ModifiedBy)
                .WithMany()
                .HasForeignKey(s => s.ModifiedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
