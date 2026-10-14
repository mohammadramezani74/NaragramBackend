namespace CleanArchitecture.Domain.Enums
{
    /// <summary>چه کسانی یک ربات را می‌بینند و می‌توانند با آن کار کنند.</summary>
    public enum BotAccessMode
    {
        All = 0,
        Roles = 1,
        Users = 2
    }

    /// <summary>گره منو یا زیرمنو دارد، یا یک جریان از کد را اجرا می‌کند.</summary>
    public enum BotMenuNodeKind
    {
        Menu = 0,
        Flow = 1
    }

    /// <summary>
    /// نحوه‌ی نمایش گزینه‌های یک زیرمنو.
    /// Inline: دکمه‌های شیشه‌ای زیر پیام؛ با هر انتخاب همان پیام عوض می‌شود.
    /// Keyboard: دکمه‌ها جای کیبورد پایین صفحه؛ با هر انتخاب کیبورد عوض می‌شود.
    /// </summary>
    public enum BotMenuDisplay
    {
        Inline = 0,
        Keyboard = 1
    }
}
