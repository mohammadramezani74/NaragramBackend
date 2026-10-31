namespace CleanArchitecture.Application.Abstraction.Authentication;

/// <summary>
/// چه کسی مدیر است و چه دسترسی‌هایی دارد.
/// مالک‌ها: فهرست Admin:UserIds در تنظیمات سرور (همه‌ی دسترسی‌ها، از پنل قابل تغییر نیست).
/// بقیه‌ی مدیران: کسانی که مالک در صفحه‌ی «مدیران» دسترسی داده.
/// </summary>
public interface IAdminAccess
{
    /// <summary>مالک یا مدیری با حداقل یک دسترسی.</summary>
    bool IsAdmin(Guid? userId);

    bool IsOwner(Guid? userId);

    /// <summary>دسترسی‌های کاربر؛ برای مالک همه، برای غیرمدیر خالی.</summary>
    IReadOnlySet<string> PermissionsOf(Guid? userId);

    bool Has(Guid? userId, string permission);

    /// <summary>کاربر درخواست جاری ادمین است؟ بدون ورود همیشه false.</summary>
    bool CurrentUserIsAdmin { get; }
}
