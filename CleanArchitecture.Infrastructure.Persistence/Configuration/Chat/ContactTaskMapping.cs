using CleanArchitecture.Domain.Entities.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.Chat
{
    internal sealed class ContactTaskMapping : IEntityTypeConfiguration<ContactTask>
    {
        public void Configure(EntityTypeBuilder<ContactTask> builder)
        {
            builder.ToTable("ContactTasks");

            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).ValueGeneratedNever();

            builder.Property(t => t.Text)
                .IsRequired()
                .HasMaxLength(ContactTask.MaxTextLength);

            builder.Property(t => t.DueDate).HasColumnType("date");

            builder.Property(t => t.SourcePreview)
                .HasMaxLength(ContactTask.MaxPreviewLength);

            builder.HasOne(t => t.Owner)
                .WithMany()
                .HasForeignKey(t => t.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Contact)
                .WithMany()
                .HasForeignKey(t => t.ContactId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.CreatedByUser)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.ModifiedBy)
                .WithMany()
                .HasForeignKey(t => t.ModifiedById)
                .OnDelete(DeleteBehavior.Restrict);

            // همه‌ی خواندن‌ها با صاحب شروع می‌شوند: فهرست یک مخاطب و
            // شمارش کارهای باز برای ستون چت‌ها.
            builder.HasIndex(t => new { t.OwnerId, t.ContactId, t.IsDone })
                .HasDatabaseName("IX_ContactTasks_Owner_Contact_Done");

            // سرویس یادآوری هر چند ثانیه فقط یادآوری‌های ارسال‌نشده را می‌خواهد.
            builder.HasIndex(t => t.RemindAtUtc)
                .HasFilter("[RemindAtUtc] IS NOT NULL AND [ReminderSentAtUtc] IS NULL AND [IsDone] = 0")
                .HasDatabaseName("IX_ContactTasks_PendingReminders");
        }
    }
}
