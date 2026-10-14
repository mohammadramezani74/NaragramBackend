namespace CleanArchitecture.Application.Common.Models;

/// <summary>
/// حساب‌هایی که به بخش‌های مدیریتی (پنل ربات‌ها، نقش‌ها، خروجی کاربران) دسترسی دارند.
///
/// عمداً از نقش‌های Identity استفاده نشده: نقش را می‌شود از طریق API عوض کرد و
/// هر خطایی در آن مسیر یعنی دسترسی ادمین برای همه. این فهرست فقط در تنظیمات
/// سرور است و از داخل برنامه قابل تغییر نیست.
///
/// شناسه‌ها در appsettings.Production.json روی سرور نوشته می‌شوند (در گیت نیست)؛
/// در appsettings.json مخزن این فهرست خالی است، یعنی هیچ‌کس ادمین نیست.
/// </summary>
public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    public Guid[] UserIds { get; set; } = Array.Empty<Guid>();
}
