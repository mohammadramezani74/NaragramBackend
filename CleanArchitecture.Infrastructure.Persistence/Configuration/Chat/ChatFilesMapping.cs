using CleanArchitecture.Domain.Entities.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.Chat
{
    internal sealed class ChatFilesMapping : IEntityTypeConfiguration<ChatFiles>
    {
        public void Configure(EntityTypeBuilder<ChatFiles> builder)
        {
            builder.ToTable("ChatFiles");
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
            // رابطه‌ی چند‌به‌چند از طریق جدول واسط MessageFiles.
            //
            // نکته‌ی مهم: این یک «skip navigation» است، یعنی در کد همچنان
            // message.ChatFiles یک ICollection<ChatFiles> ساده است. به همین
            // دلیل تمام Includeها، پروجکشن‌ها و کوئری‌های موجود بدون تغییر کار
            // می‌کنند و EF خودش موقع ذخیره‌ی پیام ردیف واسط را می‌سازد.
            //
            // هر دو سمت Restrict است تا حذف پیام یا فایل بدون پاک کردن ردیف
            // واسط با خطا شکست بخورد، نه اینکه بی‌صدا داده را یتیم کند.
            builder.HasMany(f => f.Messages)
                   .WithMany(m => m.ChatFiles)
                   .UsingEntity<MessageFile>(
                        right => right.HasOne(mf => mf.Message)
                                      .WithMany()
                                      .HasForeignKey(mf => mf.MessageId)
                                      .OnDelete(DeleteBehavior.Restrict),
                        left => left.HasOne(mf => mf.ChatFile)
                                    .WithMany()
                                    .HasForeignKey(mf => mf.ChatFileId)
                                    .OnDelete(DeleteBehavior.Restrict),
                        join =>
                        {
                            join.ToTable("MessageFiles");
                            join.HasKey(x => new { x.MessageId, x.ChatFileId });
                            join.Property(x => x.CreateDate).HasColumnType("datetime2");
                            join.HasIndex(x => x.ChatFileId);
                        });
        }
    }
}
