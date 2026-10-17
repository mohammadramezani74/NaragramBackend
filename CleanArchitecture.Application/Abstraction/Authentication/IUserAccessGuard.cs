namespace CleanArchitecture.Application.Abstraction.Authentication
{
    /// <summary>
    /// تصمیم این‌که توکنِ معتبرِ یک کاربر هنوز پذیرفته شود یا نه: حساب غیرفعال، یا توکنی که قبل از
    /// «خروج از همه‌ی دستگاه‌ها» ساخته شده، رد می‌شود. توکن دسترسی ۷ روز اعتبار دارد، پس بدون این
    /// چک غیرفعال کردن تا یک هفته اثر نداشت. وضعیت در حافظه نگه داشته می‌شود (فهرست کوچک است).
    /// </summary>
    public interface IUserAccessGuard
    {
        Task<bool> IsAllowedAsync(Guid userId, DateTime issuedAtUtc, CancellationToken ct = default);

        void MarkDisabled(Guid userId, DateTimeOffset at);

        void MarkEnabled(Guid userId);

        void MarkSessionsRevoked(Guid userId, DateTimeOffset at);
    }
}
