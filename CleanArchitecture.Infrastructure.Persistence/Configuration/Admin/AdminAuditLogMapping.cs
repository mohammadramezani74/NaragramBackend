using CleanArchitecture.Domain.Entities.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanArchitecture.Infrastructure.Persistence.Configuration.Admin
{
    internal sealed class AdminAuditLogMapping : IEntityTypeConfiguration<AdminAuditLog>
    {
        public void Configure(EntityTypeBuilder<AdminAuditLog> builder)
        {
            builder.ToTable("AdminAuditLogs");

            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).ValueGeneratedNever();

            builder.Property(a => a.ActorName).IsRequired().HasMaxLength(AdminAuditLog.MaxNameLength);
            builder.Property(a => a.Action).IsRequired().HasMaxLength(64);
            builder.Property(a => a.TargetType).IsRequired().HasMaxLength(32);
            builder.Property(a => a.TargetName).IsRequired().HasMaxLength(AdminAuditLog.MaxNameLength);
            builder.Property(a => a.Details).HasMaxLength(AdminAuditLog.MaxDetailsLength);

            builder.HasIndex(a => a.CreateDate);
            builder.HasIndex(a => new { a.TargetId, a.CreateDate });

            // اقدام‌کننده فقط شناسه است (بدون FK) تا حذف یا تغییر کاربر رویداد را نبرد.
            builder.Ignore(a => a.CreatedByUser);
            builder.Ignore(a => a.ModifiedBy);
        }
    }
}
