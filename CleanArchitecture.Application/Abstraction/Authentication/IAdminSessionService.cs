namespace CleanArchitecture.Application.Abstraction.Authentication;

public sealed record AdminCodeResult(bool Ok, string Message, string? MaskedPhone, int RetryAfterSeconds);

public sealed record AdminVerifyResult(bool Ok, string Message, string? Token, int IdleMinutes);

/// <summary>
/// تأیید دوم ورود به پنل مدیریت با کد پیامکی و نشست جدای پنل.
/// توکن چت به‌تنهایی پنل را باز نمی‌کند؛ هر درخواست پنل باید سرآیند
/// X-Admin-Session با یک نشست معتبر همین کاربر داشته باشد.
/// نشست‌ها در حافظه‌ی سرورند؛ با ری‌استارت سرور همه دوباره کد می‌خواهند.
/// </summary>
public interface IAdminSessionService
{
    public const string HeaderName = "X-Admin-Session";

    Task<AdminCodeResult> SendCodeAsync(Guid userId, CancellationToken ct);

    Task<AdminVerifyResult> VerifyAsync(Guid userId, string code, CancellationToken ct);

    /// <summary>نشست معتبر و مال همین کاربر است؟ (زمان بیکاری را تمدید می‌کند)</summary>
    bool Validate(string? token, Guid userId);

    void Revoke(string? token);

    /// <summary>همه‌ی نشست‌های پنل این کاربر را می‌بندد و تعدادشان را برمی‌گرداند.</summary>
    int RevokeAll(Guid userId);

    int ActiveCount(Guid userId);
}
