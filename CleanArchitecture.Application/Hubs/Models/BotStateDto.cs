namespace CleanArchitecture.Application.Hubs.Models
{
    /// <summary>
    /// وضعیت ربات برای کاربر: آیا شروع کرده و الان چه کیبوردی باید ببیند.
    /// Keyboard = null یعنی کیبورد ربات بسته شود و جعبه‌ی نوشتن عادی بماند.
    /// </summary>
    public sealed record BotStateDto(
        Guid BotUserId,
        bool Started,
        List<List<string>>? Keyboard,
        string? Placeholder);
}
