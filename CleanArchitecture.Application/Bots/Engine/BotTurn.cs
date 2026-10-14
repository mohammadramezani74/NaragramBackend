using CleanArchitecture.Application.Bots.Models;
using CleanArchitecture.Domain.Entities.Bots;
using CleanArchitecture.Domain.Entities.Chat;
using CleanArchitecture.Domain.Entities.Identity;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Application.Bots.Engine
{
    /// <summary>همه‌ی چیزهایی که برای جواب دادن به یک اتفاق لازم است.</summary>
    internal sealed class BotTurn(
        Bot bot,
        User botUser,
        User human,
        Conversation conversation,
        ConversationUser humanMembership,
        BotSession session,
        BotMenu menu,
        CancellationToken cancellationToken)
    {
        public Bot Bot { get; } = bot;
        public User BotUser { get; } = botUser;
        public User Human { get; } = human;
        public Conversation Conversation { get; } = conversation;
        public ConversationUser HumanMembership { get; } = humanMembership;
        public BotSession Session { get; } = session;
        public BotMenu Menu { get; } = menu;
        public CancellationToken CancellationToken { get; } = cancellationToken;

        public string BotName => (BotUser.FirsName ?? Bot.Handle).Trim();
    }

    /// <summary>درخت منوی یک ربات در حافظه.</summary>
    internal sealed class BotMenu
    {
        public const string BackLabel = "« بازگشت";

        private readonly List<BotMenuNode> _nodes;

        public BotMenu(IEnumerable<BotMenuNode> nodes)
        {
            _nodes = nodes.OrderBy(n => n.SortOrder).ToList();
        }

        public BotMenuNode? Root => _nodes.FirstOrDefault(n => n.ParentId == null);

        public BotMenuNode? Find(Guid id) => _nodes.FirstOrDefault(n => n.Id == id);

        public BotMenuNode? Parent(BotMenuNode node) => node.ParentId is Guid p ? Find(p) : null;

        public List<BotMenuNode> Children(Guid id) => _nodes.Where(n => n.ParentId == id).ToList();

        public BotMenuNode? ChildByLabel(Guid parentId, string label)
            => _nodes.FirstOrDefault(n => n.ParentId == parentId && string.Equals(n.Label, label, StringComparison.Ordinal));

        /// <summary>«درخواست‌ها › مساعده › مساعده‌ی عادی» (بدون ریشه).</summary>
        public string Path(BotMenuNode node)
        {
            var labels = new List<string>();
            var current = node;
            var guard = 0;
            while (current is not null && current.ParentId is not null && guard++ < 20)
            {
                labels.Insert(0, current.Label);
                current = Parent(current);
            }
            return string.Join(" › ", labels);
        }

        public static string NodeData(Guid nodeId) => "m:" + nodeId.ToString("N");

        public static bool TryParseNodeData(string data, out Guid nodeId)
        {
            nodeId = Guid.Empty;
            return data.StartsWith("m:", StringComparison.Ordinal)
                   && Guid.TryParseExact(data[2..], "N", out nodeId);
        }

        /// <summary>دکمه‌های شیشه‌ای یک زیرمنو: دو دکمه در هر ردیف، «بازگشت» تمام عرض.</summary>
        public BotReplyMarkup InlineMarkup(BotMenuNode menu)
        {
            var markup = new BotReplyMarkup
            {
                Rows = Children(menu.Id)
                    .Select(c => new BotButton(c.Label, NodeData(c.Id)))
                    .Chunk(2)
                    .Select(r => r.ToList())
                    .ToList(),
            };

            if (Parent(menu) is { } parent)
                markup.Rows.Add(new List<BotButton> { new(BackLabel, NodeData(parent.Id), "back") });

            return markup;
        }

        /// <summary>کیبورد یک زیرمنو: دو دکمه در هر ردیف، «بازگشت» در ردیف آخر.</summary>
        public List<List<string>> KeyboardRows(BotMenuNode menu)
        {
            var rows = Children(menu.Id)
                .Select(c => c.Label)
                .Chunk(2)
                .Select(r => r.ToList())
                .ToList();

            if (menu.ParentId is not null)
                rows.Add(new List<string> { BackLabel });

            return rows;
        }

        public bool IsKeyboard(BotMenuNode node) => node.Kind == BotMenuNodeKind.Menu && node.Display == BotMenuDisplay.Keyboard;
    }

    /// <summary>کیبوردی که الان برای کاربر نمایش داده می‌شود (BotSession.KeyboardJson).</summary>
    public sealed record BotKeyboardState(List<List<string>> Rows, string? Placeholder);
}
