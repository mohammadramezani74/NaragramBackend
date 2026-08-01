using CleanArchitecture.Domain.Entities.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.Chat
{
    public sealed class MessageMapping : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("Messages"); 

            builder.Property(m => m.Id).ValueGeneratedNever();
            builder.HasIndex(m => new { m.ConversationId, m.IsPinned })
       .HasFilter("[IsPinned] = 1");

            builder.HasIndex(m => new { m.ChannelId, m.IsPinned })
                   .HasFilter("[IsPinned] = 1");
            builder.Property(m => m.Content)
                .IsRequired()
                .HasMaxLength(4096);

            builder.Property(m => m.CreateDate).IsRequired();
            builder.Property(m => m.Deleted).IsRequired();

            builder.Property<string>(AuditableShadowProperties.CreatedByBrowserName).HasMaxLength(1000);
            builder.Property<string>(AuditableShadowProperties.ModifiedByBrowserName).HasMaxLength(1000);
            builder.Property<string>(AuditableShadowProperties.CreatedByIp).HasMaxLength(255);
            builder.Property<string>(AuditableShadowProperties.ModifiedByIp).HasMaxLength(255);

            builder.HasOne(m => m.CreatedByUser)
                .WithMany()
                .HasForeignKey(m => m.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.ModifiedBy)
                .WithMany()
                .HasForeignKey(m => m.ModifiedById)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(m => m.Channel)
                    .WithMany()
                    .HasForeignKey(m => m.ChannelId)
                    .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(m => new { m.ChannelId, m.CreateDate });
            builder.HasIndex(m => new { m.ConversationId, m.CreateDate });

            builder.HasOne(m => m.ParentMessage)
                    .WithMany(m => m.Replies)
                    .HasForeignKey(m => m.ParentMessageId)
                    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
