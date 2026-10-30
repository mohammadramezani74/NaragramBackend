using System.Globalization;
using System.Text.Json;

namespace CleanArchitecture.Application.Bots.Hr
{
    // ======================================================================
    // کارتابل INTS (متدهای GetKartable، KartableHistory و KartableDecide ماژول ناراگرام).
    // هر چیزی که به شکل JSON کارتابل بستگی دارد همین‌جاست. مدل‌ها داخل CallAsync جریان
    // ذخیره می‌شوند، پس JSON‌پذیرند.
    // ======================================================================

    /// <param name="Type">warranty | personnel | goods | suggestion</param>
    /// <param name="SendTime">زمان رسیدن به کارتابل (ساعت سرور INTS).</param>
    public sealed record KartableItem(
        string Type,
        Guid Id,
        string DocNo,
        string DocType,
        string Subject,
        string Sender,
        string Status,
        string Description,
        DateTime SendTime,
        string PanelLink);

    /// <param name="IsOpen">این مرحله هنوز منتظر بررسی است.</param>
    public sealed record KartableStep(string From, string To, string Status, string Comment, DateTime Time, bool IsOpen);

    /// <param name="MoneyRial">اگر پر باشد، مقدار مبلغ به ریال است و به تومان نمایش داده می‌شود.</param>
    public sealed record KartableField(string Label, string Value, decimal? MoneyRial);

    /// <param name="Insights">نکته‌های آماده‌ی نمایش که INTS از داده‌ی خودش درآورده.</param>
    /// <param name="MustRefer">تایید/رد مستقیم ممکن نیست؛ فقط ارجاع (پیشنهاد، وقتی عضو کمیته نیستید).</param>
    public sealed record KartableDetail(List<KartableField> Fields, List<string> Insights, bool CanRefer, bool MustRefer);

    public sealed record KartablePerson(Guid Id, string Name);

    public sealed record KartableCount(string Type, int Count);

    /// <param name="MedianHours">میانه‌ی فاصله‌ی رسیدن به کارتابل تا تصمیم (ساعت).</param>
    public sealed record KartableWeekly(
        int Total, int Approved, int Rejected, double? MedianHours,
        int PrevTotal, double? PrevMedianHours, List<KartableCount> ByType);

    public static class KartableApi
    {
        public static async Task<HrResult<List<KartableItem>>> GetAsync(IIntsConnector ints, string? phone, CancellationToken ct)
        {
            var r = await ints.SendAsync(HttpMethod.Get, "GetKartable", phone ?? "", null, ct);
            if (!r.Ok) return HrResult<List<KartableItem>>.Fail(r.Message);

            var list = new List<KartableItem>();
            foreach (var e in Array(r.Data))
            {
                if (!Guid.TryParse(HrApi.Str(e, "id"), out var id)) continue;
                var type = HrApi.Str(e, "type") ?? "";
                if (!KartableTypes.IsKnown(type)) continue;
                list.Add(new KartableItem(
                    type,
                    id,
                    Text(e, "docNo"),
                    Text(e, "docType"),
                    Text(e, "subject"),
                    Text(e, "sender"),
                    Text(e, "status"),
                    Text(e, "description"),
                    Date(e, "sendTime"),
                    HrApi.Str(e, "panelLink") ?? ""));
            }
            return HrResult<List<KartableItem>>.Success(list);
        }

        public static async Task<HrResult<List<KartableStep>>> GetHistoryAsync(IIntsConnector ints, string? phone, string type, Guid id, CancellationToken ct)
        {
            var r = await ints.SendAsync(HttpMethod.Get, $"{Uri.EscapeDataString(type)}/{id}/KartableHistory", phone ?? "", null, ct);
            if (!r.Ok) return HrResult<List<KartableStep>>.Fail(r.Message);

            var list = Array(r.Data)
                .Select(e => new KartableStep(
                    Text(e, "from"),
                    Text(e, "to"),
                    Text(e, "status"),
                    Text(e, "comment"),
                    Date(e, "time"),
                    HrApi.Str(e, "isOpen") == "true"))
                .ToList();
            return HrResult<List<KartableStep>>.Success(list);
        }

        public static async Task<HrResult<KartableDetail>> GetDetailAsync(IIntsConnector ints, string? phone, string type, Guid id, CancellationToken ct)
        {
            var r = await ints.SendAsync(HttpMethod.Get, $"{Uri.EscapeDataString(type)}/{id}/KartableDetail", phone ?? "", null, ct);
            if (!r.Ok) return HrResult<KartableDetail>.Fail(r.Message);
            if (r.Data is not { ValueKind: JsonValueKind.Object } obj)
                return HrResult<KartableDetail>.Fail("جزئیات قابل خواندن نبود.");

            var fields = Array(Prop(obj, "fields"))
                .Select(e =>
                {
                    decimal? money = decimal.TryParse(HrApi.Str(e, "moneyRial"), NumberStyles.Number, CultureInfo.InvariantCulture, out var m) ? m : null;
                    return new KartableField(Text(e, "label"), Text(e, "value"), money);
                })
                .Where(f => f.Label.Length > 0 && (f.Value.Length > 0 || f.MoneyRial is not null))
                .ToList();
            var insights = Array(Prop(obj, "insights"))
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => HrText.Clean(e.GetString()) ?? "")
                .Where(t => t.Length > 0)
                .ToList();
            return HrResult<KartableDetail>.Success(new KartableDetail(
                fields, insights, HrApi.Str(obj, "canRefer") == "true", HrApi.Str(obj, "mustRefer") == "true"));
        }

        public static async Task<HrResult<KartableWeekly>> GetWeeklyAsync(IIntsConnector ints, string? phone, CancellationToken ct)
        {
            var r = await ints.SendAsync(HttpMethod.Get, "KartableWeekly", phone ?? "", null, ct);
            if (!r.Ok) return HrResult<KartableWeekly>.Fail(r.Message);
            if (r.Data is not { ValueKind: JsonValueKind.Object } obj)
                return HrResult<KartableWeekly>.Fail("کارنامه قابل خواندن نبود.");

            var byType = Array(Prop(obj, "byType"))
                .Select(e => new KartableCount(HrApi.Str(e, "type") ?? "", Int(e, "count")))
                .Where(c => KartableTypes.IsKnown(c.Type) && c.Count > 0)
                .ToList();
            return HrResult<KartableWeekly>.Success(new KartableWeekly(
                Int(obj, "total"), Int(obj, "approved"), Int(obj, "rejected"), Dbl(obj, "medianHours"),
                Int(obj, "prevTotal"), Dbl(obj, "prevMedianHours"), byType));
        }

        public static async Task<HrResult<List<KartablePerson>>> SearchPersonnelAsync(IIntsConnector ints, string? phone, string query, CancellationToken ct)
        {
            var r = await ints.SendAsync(HttpMethod.Get, "SearchPersonnel?q=" + Uri.EscapeDataString(query), phone ?? "", null, ct);
            if (!r.Ok) return HrResult<List<KartablePerson>>.Fail(r.Message);
            var list = Array(r.Data)
                .Select(e => Guid.TryParse(HrApi.Str(e, "id"), out var id) ? new KartablePerson(id, Text(e, "name")) : null)
                .Where(p => p is not null && p.Name.Length > 0)
                .Select(p => p!)
                .ToList();
            return HrResult<List<KartablePerson>>.Success(list);
        }

        /// <summary>
        /// تایید، رد یا (فقط پیشنهاد) ارجاع به referTo. جواب INTS همان OperationResult پنل است:
        /// { isSucceded, message }.
        /// </summary>
        public static async Task<HrResult<bool>> DecideAsync(
            IIntsConnector ints, string? phone, string type, Guid id, bool approve, string comment, CancellationToken ct, Guid? referTo = null)
        {
            var body = new Dictionary<string, object?>
            {
                ["type"] = type,
                ["id"] = id,
                ["approve"] = approve,
                ["comment"] = comment,
                ["referTo"] = referTo,
            };
            var r = await ints.SendAsync(HttpMethod.Post, "KartableDecide", phone ?? "", body, ct);
            if (!r.Ok) return HrResult<bool>.Fail(r.Message);
            if (r.Data is { ValueKind: JsonValueKind.Object } obj
                && HrApi.Str(obj, "isSucceded") == "false")
                return HrResult<bool>.Fail(HrText.Clean(HrApi.Str(obj, "message")) ?? "انجام نشد.");
            return HrResult<bool>.Success(true);
        }

        // ------------------------------------------------------------ JSON

        /// <summary>آرایه‌ی مستقیم، یا { result: [...] } اگر INTS آن را در OperationResult پیچید.</summary>
        private static IEnumerable<JsonElement> Array(JsonElement? data)
        {
            if (data is not { } d) return Enumerable.Empty<JsonElement>();
            if (d.ValueKind == JsonValueKind.Array) return d.EnumerateArray().ToList();
            if (d.ValueKind == JsonValueKind.Object)
                foreach (var p in d.EnumerateObject())
                    if (p.Value.ValueKind == JsonValueKind.Array && string.Equals(p.Name, "result", StringComparison.OrdinalIgnoreCase))
                        return p.Value.EnumerateArray().ToList();
            return Enumerable.Empty<JsonElement>();
        }

        private static JsonElement? Prop(JsonElement obj, string name)
        {
            foreach (var p in obj.EnumerateObject())
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) return p.Value;
            return null;
        }

        private static int Int(JsonElement e, string name)
            => int.TryParse(HrApi.Str(e, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

        private static double? Dbl(JsonElement e, string name)
            => double.TryParse(HrApi.Str(e, name), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;

        private static string Text(JsonElement e, string name) => HrText.Clean(HrApi.Str(e, name)) ?? "";

        private static DateTime Date(JsonElement e, string name)
            => DateTime.TryParse(HrApi.Str(e, name), CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : default;
    }

    /// <summary>نوع‌های کارتابل؛ همان رشته‌هایی که NaragramKartableService در INTS برمی‌گرداند.</summary>
    public static class KartableTypes
    {
        public const string Warranty = "warranty";
        public const string Personnel = "personnel";
        public const string Goods = "goods";
        public const string Suggestion = "suggestion";

        public sealed record Info(string Type, string Title, string Plural, string Icon, string[] RejectReasons);

        public static readonly Info[] All =
        {
            new(Warranty, "ضمانت‌نامه", "ضمانت‌نامه", "🛡",
                new[] { "مدارک ضمانت‌نامه ناقص است", "مبلغ با قرارداد هم‌خوانی ندارد", "تاریخ اعتبار باید اصلاح شود" }),
            new(Personnel, "درخواست پرسنلی", "درخواست پرسنلی", "👥",
                new[] { "مدارک یا اطلاعات ناقص است", "مبلغ بیشتر از سقف مجاز است", "فعلاً امکان‌پذیر نیست" }),
            new(Goods, "درخواست کالا", "درخواست کالا", "📦",
                new[] { "بودجه‌ی این دوره تمام شده", "کالای مشابه موجود است", "توضیح بیشتری لازم است" }),
            new(Suggestion, "پیشنهاد", "پیشنهاد", "💡",
                new[] { "قبلاً مشابهش اجرا شده", "فعلاً در اولویت نیست", "نیاز به بررسی کارشناسی بیشتر دارد" }),
        };

        public static bool IsKnown(string type) => All.Any(i => i.Type == type);

        public static Info Of(string type) => All.FirstOrDefault(i => i.Type == type) ?? All[0];
    }
}
