namespace CleanArchitecture.Application.Abstraction.Authentication;

/// <summary>
/// پاسخ به «آیا این کاربر ادمین است؟» بر اساس فهرست AdminOptions.
/// </summary>
public interface IAdminAccess
{
    bool IsAdmin(Guid? userId);

    /// <summary>کاربر درخواست جاری ادمین است؟ بدون ورود همیشه false.</summary>
    bool CurrentUserIsAdmin { get; }
}
