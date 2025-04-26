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
    internal sealed class MessageReactionMapping : IEntityTypeConfiguration<MessageReaction>
    {
        public void Configure(EntityTypeBuilder<MessageReaction> builder)
        {
            builder.ToTable("MessageReactions");

            builder.Property(m => m.Id).ValueGeneratedNever();

            builder.Property(m => m.Reaction)
                .IsRequired()
                .HasMaxLength(12);

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

            builder.HasOne(m => m.Message)
                    .WithMany(m => m.Reactions)
                    .HasForeignKey(m => m.MessageId)
                    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
