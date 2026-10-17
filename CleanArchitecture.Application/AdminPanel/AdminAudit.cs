using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Admin;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.AdminPanel
{
    public sealed record AdminAuditItem(DateTime At, string ActorName, string Action, string Text, string TargetName, string? Details);

    /// <summary>کمک‌های مشترک پنل ادمین: دسترسی، نام اقدام‌کننده، ثبت رویداد، ماسک شماره.</summary>
    internal static class AdminPanelKit
    {
        public static OperationResult Denied() => new OperationResult().NotFound("یافت نشد.");

        public static async Task<(Guid Id, string Name)> ActorAsync(IApplicationUserManager users, IApplicationUnitOfWork uow, CancellationToken ct)
        {
            var id = users.UserId ?? Guid.Empty;
            var me = await uow.Users.AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new { u.FirsName, u.LastName })
                .FirstOrDefaultAsync(ct);
            return (id, me is null ? "ادمین" : Name(me.LastName, me.FirsName));
        }

        /// <summary>رویداد را به Change Tracker اضافه می‌کند؛ با SaveChanges همان کار ذخیره می‌شود.</summary>
        public static void Audit(IApplicationUnitOfWork uow, (Guid Id, string Name) actor, string action, string targetType, Guid? targetId, string targetName, string? details = null)
            => uow.AdminAuditLogs.Add(AdminAuditLog.Create(actor.Id, actor.Name, action, targetType, targetId, targetName, details));

        public static async Task<List<AdminAuditItem>> LogForAsync(IApplicationUnitOfWork uow, Guid targetId, int take, CancellationToken ct)
        {
            var rows = await uow.AdminAuditLogs.AsNoTracking()
                .Where(a => a.TargetId == targetId)
                .OrderByDescending(a => a.CreateDate)
                .Take(take)
                .Select(a => new { a.CreateDate, a.ActorName, a.Action, a.TargetName, a.Details })
                .ToListAsync(ct);
            return rows.Select(a => new AdminAuditItem(a.CreateDate, a.ActorName, a.Action, ActionText(a.Action), a.TargetName, a.Details)).ToList();
        }

        public static string ActionText(string action) => action switch
        {
            "user.disable" => "حساب غیرفعال شد",
            "user.enable" => "حساب دوباره فعال شد",
            "user.sessions" => "از همه‌ی دستگاه‌ها خارج شد",
            "user.phone" => "شماره‌ی موبایل نمایش داده شد",
            "user.roles" => "نقش‌ها تغییر کرد",
            "user.rename" => "نام تغییر کرد",
            "room.edit" => "نام یا توضیح تغییر کرد",
            "broadcast.create" => "پیام همگانی ثبت شد",
            "settings.announcement" => "اعلان بالای برنامه تغییر کرد",
            "settings.creation" => "اجازه‌ی ساخت گروه و کانال تغییر کرد",
            "broadcast.cancel" => "پیام همگانی لغو شد",
            "role.create" => "نقش ساخته شد",
            "role.rename" => "نام نقش عوض شد",
            "role.delete" => "نقش حذف شد",
            "room.archive" => "بایگانی شد",
            "room.restore" => "از بایگانی برگشت",
            "room.remove-member" => "عضو بیرون شد",
            "bot.key.create" => "کلید API ربات ساخته شد",
            "bot.key.revoke" => "کلید API ربات باطل شد",
            "bot.official" => "حالت رسمی ربات تغییر کرد",
            _ => action,
        };

        // همان قرار بقیه‌ی برنامه: «LastName FirsName»
        public static string Name(string? lastName, string? firstName)
        {
            var name = string.Join(" ", new[] { lastName, firstName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
            return name.Length == 0 ? "بی‌نام" : name;
        }

        public static string MaskPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return "—";
            phone = phone.Trim();
            return phone.Length < 8 ? new string('•', phone.Length) : phone[..4] + "•••" + phone[^4..];
        }

        /// <summary>ارقام فارسی/عربی جستجو را لاتین می‌کند تا با شماره‌ها جور شود.</summary>
        public static string LatinDigits(string text)
            => new(text.Select(c => c is >= '۰' and <= '۹' ? (char)('0' + (c - '۰')) : c is >= '٠' and <= '٩' ? (char)('0' + (c - '٠')) : c).ToArray());
    }
}
