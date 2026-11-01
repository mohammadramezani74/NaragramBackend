using CleanArchitecture.Domain.Entities.Admin;
using CleanArchitecture.Infrastructure.Persistence.Encryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.Admin
{
    internal sealed class AppFeedbackMapping : IEntityTypeConfiguration<AppFeedback>
    {
        public void Configure(EntityTypeBuilder<AppFeedback> builder)
        {
            builder.ToTable("AppFeedbacks");

            builder.HasKey(f => f.Id);
            builder.Property(f => f.Id).ValueGeneratedNever();

            // شماره‌ی پیگیری را دیتابیس می‌دهد (۱، ۲، ۳ ...)
            builder.Property(f => f.Number).UseIdentityColumn();
            builder.Property(f => f.Number).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            builder.HasIndex(f => f.Number).IsUnique();

            builder.Property(f => f.Kind).IsRequired().HasMaxLength(16);
            builder.Property(f => f.Status).IsRequired().HasMaxLength(16);
            builder.Property(f => f.RepliedByName).HasMaxLength(200);

            // متن کاربر و جواب ادمین مثل پیام‌ها رمزنگاری می‌شوند
            builder.Property(f => f.Text).IsRequired().HasConversion(EncryptedStringConverter.Instance);
            builder.Property(f => f.Reply).HasConversion(EncryptedStringConverter.Instance);

            builder.HasIndex(f => new { f.Status, f.CreatedAt });
            builder.HasIndex(f => f.UserId);
        }
    }
}
