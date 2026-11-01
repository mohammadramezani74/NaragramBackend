namespace CleanArchitecture.Domain.Entities.Bots
{
    /// <summary>
    /// یک بار اجرای یک جریان ربات (مثلاً «فیش حقوقی») برای یک کاربر؛ فقط برای گزارش‌های پنل.
    /// موقع شروع جریان ساخته می‌شود و موقع تمام شدن، لغو یا خطا بسته می‌شود. جریانی که
    /// کاربر وسطش رها کرده باز می‌ماند (EndedAt خالی) و در گزارش «نیمه‌کاره» شمرده می‌شود.
    /// </summary>
    public sealed class BotFlowRun
    {
        public const int MaxFlowKeyLength = 100;
        public const int MaxOutcomeLength = 16;

        public const string Running = "running";
        public const string Done = "done";
        public const string Cancelled = "cancelled";
        public const string Failed = "failed";
        public const string Expired = "expired";

        private BotFlowRun() { }

        public Guid Id { get; private set; }
        public Guid BotId { get; private set; }
        public Guid UserId { get; private set; }
        public string FlowKey { get; private set; } = string.Empty;

        /// <summary>گزینه‌ی منویی که جریان را شروع کرد (اگر از منو بود).</summary>
        public Guid? NodeId { get; private set; }

        public DateTime StartedAt { get; private set; }
        public DateTime? EndedAt { get; private set; }
        public string Outcome { get; private set; } = Running;

        public static BotFlowRun Start(Guid botId, Guid userId, string flowKey, Guid? nodeId) => new()
        {
            Id = Guid.NewGuid(),
            BotId = botId,
            UserId = userId,
            FlowKey = flowKey.Length > MaxFlowKeyLength ? flowKey[..MaxFlowKeyLength] : flowKey,
            NodeId = nodeId,
            StartedAt = DateTime.Now,
        };

        public void End(string outcome)
        {
            if (EndedAt is not null)
                return;
            Outcome = outcome;
            EndedAt = DateTime.Now;
        }
    }
}
