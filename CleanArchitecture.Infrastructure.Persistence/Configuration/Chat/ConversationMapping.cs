using CleanArchitecture.Domain.Entities.Chat;
using CleanArchitecture.Domain.ValueObjects.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel;
using System.Reflection.Emit;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.Chat;

internal sealed class ConversationMapping : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {


        builder.HasKey(c => c.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
        builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ModifiedBy).WithMany().HasForeignKey(x => x.ModifiedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Messages)
     .WithOne(x => x.Conversation) 
     .HasForeignKey(x => x.ConversationId);
        //builder.OwnsMany<Message>(x => x.Messages, message =>
        //{
        //    message.Property<string>(AuditableShadowProperties.CreatedByBrowserName).HasMaxLength(1000);
        //    message.Property<string>(AuditableShadowProperties.ModifiedByBrowserName).HasMaxLength(1000);
        //    message.Property<string>(AuditableShadowProperties.CreatedByIp).HasMaxLength(255);
        //    message.Property<string>(AuditableShadowProperties.ModifiedByIp).HasMaxLength(255);
        //    message.HasKey(m => m.Id);
        //    message.Property(x=>x.Id).ValueGeneratedNever();

        //    message.Property(m => m.Content)
        //        .IsRequired()
        //        .HasMaxLength(4096); 

        //    message.Property(m => m.CreateDate)
        //        .IsRequired();

        //    message.Property(m => m.Deleted)
        //        .IsRequired();

         
        //    message.HasOne(m => m.CreatedByUser)
        //        .WithMany()
        //        .HasForeignKey(m => m.CreatedByUserId)
        //        .OnDelete(DeleteBehavior.Restrict);

       
        //    message.HasOne(m => m.ModifiedBy)
        //        .WithMany()
        //        .HasForeignKey(m => m.ModifiedById)
        //        .OnDelete(DeleteBehavior.Restrict);

        //});

        builder.OwnsMany<ConversationUser>(x => x.Users, conversationUser =>
        {
            conversationUser.Property<string>(AuditableShadowProperties.CreatedByBrowserName).HasMaxLength(1000);
            conversationUser.Property<string>(AuditableShadowProperties.ModifiedByBrowserName).HasMaxLength(1000);
            conversationUser.Property<string>(AuditableShadowProperties.CreatedByIp).HasMaxLength(255);
            conversationUser.Property<string>(AuditableShadowProperties.ModifiedByIp).HasMaxLength(255);
            conversationUser.HasKey(cu => cu.Id);

       
            conversationUser.HasOne(cu => cu.CreatedByUser)
                .WithMany()
                .HasForeignKey(cu => cu.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

         
            conversationUser.HasOne(cu => cu.ModifiedBy)
                .WithMany()
                .HasForeignKey(cu => cu.ModifiedById)
                .OnDelete(DeleteBehavior.Restrict);

      
            conversationUser.HasOne(cu => cu.User)
                .WithMany()
                .HasForeignKey(cu => cu.UserId)
                .OnDelete(DeleteBehavior.Restrict);

        
         
        });
    }
}
