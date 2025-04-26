using CleanArchitecture.Domain.Entities.Chat;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.Chat
{
    internal class UserAvatarMapping : IEntityTypeConfiguration<UserAvatar>
    {
        public void Configure(EntityTypeBuilder<UserAvatar> builder)
        {
            builder.ToTable("UserAvatars");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();


            builder.Property(x => x.Extension).HasColumnType("varchar").HasMaxLength(10);
            builder.Property(x => x.FileName).HasColumnType("nvarchar").HasMaxLength(200);
           
            builder.HasOne(m => m.CreatedByUser)
                   .WithMany()
                   .HasForeignKey(m => m.CreatedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.ModifiedBy)
                .WithMany()
                .HasForeignKey(m => m.ModifiedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x=>x.User)
                .WithMany(x=>x.UserAvatars)
                .HasForeignKey(x=>x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
