using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CleanArchitecture.Application.Bots.Models
{
    /// <summary>
    /// چیزهایی که زیر متن پیام ربات نمایش داده می‌شود: ردیف‌های دکمه‌ی شیشه‌ای،
    /// کارت خلاصه (برچسب/مقدار) و یک یادداشت کوتاه (مثلاً «تأیید شد» یا مسیر منو).
    /// به صورت JSON در Message.ReplyMarkup ذخیره و عیناً به فرانت داده می‌شود.
    /// </summary>
    public sealed class BotReplyMarkup
    {
        public List<List<BotButton>> Rows { get; set; } = new();
        public List<BotCardRow>? Card { get; set; }
        public string? Note { get; set; }

        /// <summary>ok (سبز) یا muted (خاکستری).</summary>
        public string? NoteTone { get; set; }

        [JsonIgnore]
        public bool IsEmpty => Rows.Count == 0 && (Card is null || Card.Count == 0) && string.IsNullOrEmpty(Note);

        public bool ContainsData(string data)
            => Rows.Any(r => r.Any(b => string.Equals(b.Data, data, StringComparison.Ordinal)));

        public string? ToJson() => IsEmpty ? null : BotJson.Serialize(this);

        public static BotReplyMarkup? FromJson(string? json) => BotJson.Deserialize<BotReplyMarkup>(json);
    }

    /// <param name="Style">null، primary (دکمه‌ی اصلی) یا back (بازگشت، تمام عرض).</param>
    public sealed record BotButton(string Text, string Data, string? Style = null);

    public sealed record BotCardRow(string Label, string Value);

    /// <summary>یک دستور منوی «/» که به یک گره منو اشاره می‌کند.</summary>
    public sealed record BotCommandItem(string Command, string Description, Guid NodeId);

    public static class BotJson
    {
        public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

        public static T? Deserialize<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default;
            try
            {
                return JsonSerializer.Deserialize<T>(json, Options);
            }
            catch (JsonException)
            {
                return default;
            }
        }
    }
}
