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
        builder.Property(x => x.LastMessageText).HasMaxLength(100);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
        builder.Property(x => x.UserName).HasMaxLength(140);
        builder.Property(x => x.Description).HasMaxLength(1500);
        builder.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ModifiedBy).WithMany().HasForeignKey(x => x.ModifiedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Messages)
     .WithOne(x => x.Conversation) 
     .HasForeignKey(x => x.ConversationId);
        builder.HasMany(x=>x.Users)
            .WithOne(x=>x.Conversation)
            .HasForeignKey(x=>x.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);

        //builder.OwnsMany<ConversationUser>(x => x.Users, conversationUser =>
        //{
        //    conversationUser.Property<string>(AuditableShadowProperties.CreatedByBrowserName).HasMaxLength(1000);
        //    conversationUser.Property<string>(AuditableShadowProperties.ModifiedByBrowserName).HasMaxLength(1000);
        //    conversationUser.Property<string>(AuditableShadowProperties.CreatedByIp).HasMaxLength(255);
        //    conversationUser.Property<string>(AuditableShadowProperties.ModifiedByIp).HasMaxLength(255);
        //    conversationUser.HasKey(cu => cu.Id);

       
        //    conversationUser.HasOne(cu => cu.CreatedByUser)
        //        .WithMany()
        //        .HasForeignKey(cu => cu.CreatedByUserId)
        //        .OnDelete(DeleteBehavior.Restrict);

         
        //    conversationUser.HasOne(cu => cu.ModifiedBy)
        //        .WithMany()
        //        .HasForeignKey(cu => cu.ModifiedById)
        //        .OnDelete(DeleteBehavior.Restrict);

      
        //    conversationUser.HasOne(cu => cu.User)
        //        .WithMany()
        //        .HasForeignKey(cu => cu.UserId)
        //        .OnDelete(DeleteBehavior.Restrict);

        
         
        //});
    }
}
internal sealed class ConversationUserMapping : IEntityTypeConfiguration<ConversationUser>
{
    public void Configure(EntityTypeBuilder<ConversationUser> builder)
    {
        builder.ToTable("ConversationUser");
        builder.Property<string>(AuditableShadowProperties.CreatedByBrowserName).HasMaxLength(1000);
        builder.Property<string>(AuditableShadowProperties.ModifiedByBrowserName).HasMaxLength(1000);
        builder.Property<string>(AuditableShadowProperties.CreatedByIp).HasMaxLength(255);
        builder.Property<string>(AuditableShadowProperties.ModifiedByIp).HasMaxLength(255);
        builder.HasKey(cu => cu.Id);


        builder.HasOne(cu => cu.CreatedByUser)
            .WithMany()
            .HasForeignKey(cu => cu.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);


        builder.HasOne(cu => cu.ModifiedBy)
            .WithMany()
            .HasForeignKey(cu => cu.ModifiedById)
            .OnDelete(DeleteBehavior.Restrict);


        builder.HasOne(cu => cu.User)
            .WithMany()
            .HasForeignKey(cu => cu.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}