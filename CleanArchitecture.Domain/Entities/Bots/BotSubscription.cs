namespace CleanArchitecture.Domain.Entities.Bots
{
    /// <summary>
    /// اشتراک یک کاربر در پیام زمان‌بندی‌شده‌ی یک ربات (مثلاً «خلاصه‌ی صبحگاهی» دستیار).
    /// City شهری است که کاربر برای این اشتراک انتخاب کرده؛ LastSentOn جلوی ارسال دوباره در
    /// یک روز را می‌گیرد (حتی اگر سرور وسط ارسال ری‌استارت شود).
    /// </summary>
    public sealed class BotSubscription
    {
        public const int MaxTopicLength = 32;
        public const int MaxCityLength = 64;

        private BotSubscription() { }

        public Guid Id { get; private set; }
        public Guid BotId { get; private set; }
        public Guid UserId { get; private set; }
        public string Topic { get; private set; } = string.Empty;
        public string? City { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateOnly? LastSentOn { get; private set; }

        public static BotSubscription Create(Guid botId, Guid userId, string topic, string? city) => new()
        {
            Id = Guid.NewGuid(),
            BotId = botId,
            UserId = userId,
            Topic = topic,
            City = city,
            CreatedAt = DateTime.Now,
        };

        public void SetCity(string? city) => City = city;

        public void MarkSent(DateOnly day) => LastSentOn = day;
    }
}
