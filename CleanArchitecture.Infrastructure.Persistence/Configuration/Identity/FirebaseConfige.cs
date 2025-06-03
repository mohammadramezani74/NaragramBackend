using CleanArchitecture.Domain.Entities.Chat;
using CleanArchitecture.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.Identity
{
    internal class FirebaseConfige : IEntityTypeConfiguration<FireBaseToken>
    {
        public void Configure(EntityTypeBuilder<FireBaseToken> builder)
        {
            builder.ToTable("FireBaseTokens");

            builder.Property(m => m.Id).ValueGeneratedNever();
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
            builder.HasOne(x => x.User)
                .WithMany(x => x.firebaseTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
