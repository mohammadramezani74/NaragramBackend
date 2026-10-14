using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Domain.Entities.Bots
{
    /// <summary>
    /// یک گزینه در درخت منوی ربات. هر ربات دقیقاً یک ریشه دارد (ParentId = null).
    ///
    /// گره یا زیرمنو است (Kind = Menu: متن پیام + گزینه‌های فرزند) یا یک جریان از
    /// کد را با چند تنظیم اجرا می‌کند (Kind = Flow: FlowKey + ParamsJson).
    /// پنل فقط همین جدول را می‌نویسد؛ API و اعتبارسنجی داخل جریان است.
    /// </summary>
    public sealed class BotMenuNode : BaseEntity
    {
        public const int MaxLabelLength = 64;
        public const int MaxTextLength = 1000;
        public const int MaxFlowKeyLength = 100;
        public const int MaxParamsLength = 2000;

        private BotMenuNode() { }

        public Guid BotId { get; private set; }
        public Bot? Bot { get; private set; }

        public Guid? ParentId { get; private set; }

        public int SortOrder { get; private set; }

        /// <summary>متن دکمه.</summary>
        public string Label { get; private set; } = null!;

        /// <summary>برای زیرمنو: متن پیامی که بالای گزینه‌ها نمایش داده می‌شود.</summary>
        public string? Text { get; private set; }

        public BotMenuNodeKind Kind { get; private set; }

        public BotMenuDisplay Display { get; private set; }

        /// <summary>برای Kind = Flow: کلید جریان در کد (BotFlowAttribute).</summary>
        public string? FlowKey { get; private set; }

        /// <summary>تنظیم‌های جریان، شیء JSON از رشته به رشته.</summary>
        public string? ParamsJson { get; private set; }

        public static BotMenuNode CreateRoot(Guid botId, string text)
            => Create(Guid.NewGuid(), botId, null, 0, "منوی اصلی", text, BotMenuNodeKind.Menu, BotMenuDisplay.Inline, null, null);

        public static BotMenuNode Create(
            Guid id,
            Guid botId,
            Guid? parentId,
            int sortOrder,
            string label,
            string? text,
            BotMenuNodeKind kind,
            BotMenuDisplay display,
            string? flowKey,
            string? paramsJson)
        {
            return new BotMenuNode
            {
                Id = id == Guid.Empty ? Guid.NewGuid() : id,
                BotId = botId,
                ParentId = parentId,
                SortOrder = sortOrder,
                Label = Cut(label, MaxLabelLength) ?? string.Empty,
                Text = Cut(text, MaxTextLength),
                Kind = kind,
                Display = display,
                FlowKey = kind == BotMenuNodeKind.Flow ? Cut(flowKey, MaxFlowKeyLength) : null,
                ParamsJson = kind == BotMenuNodeKind.Flow ? Cut(paramsJson, MaxParamsLength) : null,
            };
        }

        /// <summary>ذخیره‌ی دوباره از پنل؛ شناسه ثابت می‌ماند تا دکمه‌های پیام‌های قبلی کار کنند.</summary>
        public void Update(
            Guid? parentId,
            int sortOrder,
            string label,
            string? text,
            BotMenuNodeKind kind,
            BotMenuDisplay display,
            string? flowKey,
            string? paramsJson)
        {
            ParentId = parentId;
            SortOrder = sortOrder;
            Label = Cut(label, MaxLabelLength) ?? string.Empty;
            Text = Cut(text, MaxTextLength);
            Kind = kind;
            Display = display;
            FlowKey = kind == BotMenuNodeKind.Flow ? Cut(flowKey, MaxFlowKeyLength) : null;
            ParamsJson = kind == BotMenuNodeKind.Flow ? Cut(paramsJson, MaxParamsLength) : null;
            ModifiedDate = DateTime.Now;
        }

        private static string? Cut(string? value, int max)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value)) return null;
            return value.Length > max ? value[..max] : value;
        }
    }
}
