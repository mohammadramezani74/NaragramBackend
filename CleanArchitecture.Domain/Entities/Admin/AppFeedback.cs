namespace CleanArchitecture.Domain.Entities.Admin
{
    /// <summary>
    /// نظر، پیشنهاد یا گزارش مشکلی که کاربر درباره‌ی خود اپ ناراگرام می‌فرستد (از ربات پشتیبانی).
    /// Number شماره‌ی پیگیری کوتاهی است که به کاربر نشان داده می‌شود. متن و جواب در دیتابیس
    /// رمزنگاری می‌شوند، مثل پیام‌ها.
    /// </summary>
    public sealed class AppFeedback
    {
        public const int MaxTextLength = 2000;
        public const int MaxReplyLength = 2000;

        public const string KindIdea = "idea";
        public const string KindBug = "bug";
        public const string KindOpinion = "opinion";

        public const string StatusNew = "new";
        public const string StatusSeen = "seen";
        public const string StatusDoing = "doing";
        public const string StatusDone = "done";
        public const string StatusRejected = "rejected";

        public static readonly string[] Kinds = { KindIdea, KindBug, KindOpinion };
        public static readonly string[] Statuses = { StatusNew, StatusSeen, StatusDoing, StatusDone, StatusRejected };

        private AppFeedback() { }

        public Guid Id { get; private set; }
        public int Number { get; private set; }
        public Guid UserId { get; private set; }

        /// <summary>رباتی که بازخورد از آن آمده؛ جواب هم از همان فرستاده می‌شود.</summary>
        public Guid? BotId { get; private set; }

        public string Kind { get; private set; } = KindIdea;
        public string Text { get; private set; } = string.Empty;
        public string Status { get; private set; } = StatusNew;
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public string? Reply { get; private set; }
        public DateTime? RepliedAt { get; private set; }
        public string? RepliedByName { get; private set; }

        public static AppFeedback Create(Guid userId, Guid? botId, string kind, string text) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BotId = botId,
            Kind = Kinds.Contains(kind) ? kind : KindOpinion,
            Text = text.Length > MaxTextLength ? text[..MaxTextLength] : text,
            CreatedAt = DateTime.Now,
        };

        public void SetStatus(string status)
        {
            if (!Statuses.Contains(status) || status == Status)
                return;
            Status = status;
            UpdatedAt = DateTime.Now;
        }

        public void SetReply(string reply, string byName)
        {
            Reply = reply.Length > MaxReplyLength ? reply[..MaxReplyLength] : reply;
            RepliedAt = DateTime.Now;
            RepliedByName = byName.Length > 200 ? byName[..200] : byName;
            UpdatedAt = RepliedAt;
            if (Status is StatusNew)
                Status = StatusSeen;
        }
    }
}
