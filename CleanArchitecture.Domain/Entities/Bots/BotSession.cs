using CleanArchitecture.Domain.Common;

namespace CleanArchitecture.Domain.Entities.Bots
{
    /// <summary>
    /// وضعیت گفتگوی یک کاربر با یک ربات (یک ردیف برای هر جفت ربات و کاربر).
    ///
    /// - StartedAt: کاربر «شروع» را زده؛ ربات تا قبل از آن پیامی نمی‌فرستد.
    /// - MenuNodeId: زیرمنویی که الان روی کیبورد پایین صفحه است (اگر هست).
    /// - Flow*: جریانی که در حال اجراست و جواب‌هایی که کاربر تا اینجا داده؛
    ///   این‌ها در دیتابیس می‌مانند تا اگر کاربر وسط کار رفت یا سرور ری‌استارت
    ///   شد، جریان از همان مرحله ادامه پیدا کند.
    /// - KeyboardJson: کیبوردی که الان برای کاربر نمایش داده می‌شود.
    /// </summary>
    public sealed class BotSession : BaseEntity
    {
        private BotSession() { }

        public Guid BotId { get; private set; }
        public Bot? Bot { get; private set; }

        public Guid UserId { get; private set; }

        public DateTime? StartedAt { get; private set; }

        public Guid? MenuNodeId { get; private set; }

        public string? FlowKey { get; private set; }
        public string? FlowParamsJson { get; private set; }
        public string? FlowStateJson { get; private set; }
        public DateTime? FlowStartedAt { get; private set; }

        public string? KeyboardJson { get; private set; }

        public DateTime LastActivityAt { get; private set; }

        public bool IsStarted => StartedAt.HasValue;
        public bool HasFlow => FlowKey is not null;

        public static BotSession Create(Guid botId, Guid userId) => new()
        {
            BotId = botId,
            UserId = userId,
            CreatedByUserId = userId,
            LastActivityAt = DateTime.Now,
        };

        /// <summary>«شروع»: همه‌چیز از اول، جریان نیمه‌تمام کنار گذاشته می‌شود.</summary>
        public void Start()
        {
            StartedAt = DateTime.Now;
            MenuNodeId = null;
            ClearFlow();
        }

        public void SetMenuNode(Guid? nodeId)
        {
            MenuNodeId = nodeId;
            Touch();
        }

        public void StartFlow(string flowKey, string? paramsJson)
        {
            FlowKey = flowKey;
            FlowParamsJson = paramsJson;
            FlowStateJson = null;
            FlowStartedAt = DateTime.Now;
            Touch();
        }

        public void SaveFlowState(string stateJson)
        {
            FlowStateJson = stateJson;
            Touch();
        }

        public void ClearFlow()
        {
            FlowKey = null;
            FlowParamsJson = null;
            FlowStateJson = null;
            FlowStartedAt = null;
            Touch();
        }

        public void SetKeyboard(string? keyboardJson)
        {
            KeyboardJson = keyboardJson;
            Touch();
        }

        public void Touch()
        {
            LastActivityAt = DateTime.Now;
            ModifiedDate = LastActivityAt;
        }
    }
}
