using System.Globalization;
using System.Text.Json;
using CleanArchitecture.Application.Bots.Flows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Bots.Assistant
{
    public sealed record PriceItem(string Key, string Title, long Toman, double? ChangePercent);

    public sealed record PriceSnapshot(bool Ok, List<PriceItem> Items, string UpdatedAt, string Sources, bool Stale);

    public interface IPriceSource : IBotConnector
    {
        /// <summary>هیچ‌وقت استثنا نمی‌دهد؛ اگر هیچ منبعی جواب نداد Ok = false.</summary>
        Task<PriceSnapshot> GetAsync(CancellationToken ct);
    }

    /// <summary>
    /// قیمت ارز، سکه، طلا و رمزارز به تومان، از منبع‌های داخلی (با قطع اینترنت بین‌الملل کار می‌کنند):
    /// ۱. BrsApi.ir اگر Assistant:BrsApiKey روی سرور تنظیم شده باشد (ثبت‌نام رایگان)،
    /// ۲. وگرنه یا اگر جواب نداد، tgju.org (فایل داده‌ی خود سایت؛ رسمی نیست و ممکن است عوض شود)،
    /// ۳. تتر و بیت‌کوین از API عمومی نوبیتکس.
    /// هر ۱۰ دقیقه یک بار گرفته می‌شود؛ اگر هیچ منبعی جواب نداد تا ۲۴ ساعت آخرین قیمت با برچسب نشان داده می‌شود.
    /// </summary>
    public sealed class MarketPriceSource(IConfiguration configuration, ILogger<MarketPriceSource> logger) : IPriceSource
    {
        private static readonly HttpClient Http = new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(10) })
        {
            Timeout = TimeSpan.FromSeconds(10),
        };

        private static readonly SemaphoreSlim Gate = new(1, 1);
        private sealed record CacheEntry(DateTime At, PriceSnapshot Snapshot);
        private static volatile CacheEntry? _cache;
        private static readonly TimeSpan Fresh = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan MaxStale = TimeSpan.FromHours(24);

        /// <summary>ترتیب و عنوان نمایش.</summary>
        public static readonly (string Key, string Title)[] Wanted =
        {
            ("usd", "دلار آمریکا"), ("eur", "یورو"), ("aed", "درهم امارات"),
            ("coin_emami", "سکه امامی"), ("coin_bahar", "سکه بهار آزادی"), ("coin_half", "نیم سکه"), ("coin_quarter", "ربع سکه"),
            ("gold18", "طلای ۱۸ عیار (گرم)"), ("mesghal", "مثقال طلا"),
            ("usdt", "تتر"), ("btc", "بیت‌کوین"),
        };

        public string Key => "assistant.prices";
        public string Title => "قیمت ارز و طلا (BrsApi / TGJU / نوبیتکس)";
        public string? Description => "منبع‌های داخلی؛ کلید BrsApi در Assistant:BrsApiKey (اختیاری).";

        public async Task<BotConnectorTestResult> TestAsync(CancellationToken cancellationToken)
        {
            var parts = new List<string>();
            var brs = await TryBrsApiAsync(cancellationToken);
            parts.Add(brs is null ? "BrsApi: " + (string.IsNullOrWhiteSpace(BrsKey) ? "کلید تنظیم نشده" : "جواب نداد") : $"BrsApi: {brs.Count} قلم");
            var tgju = await TryTgjuAsync(cancellationToken);
            parts.Add(tgju is null ? "TGJU: جواب نداد" : $"TGJU: {tgju.Count} قلم");
            var nobitex = await TryNobitexAsync(cancellationToken);
            parts.Add(nobitex is null ? "نوبیتکس: جواب نداد" : $"نوبیتکس: {nobitex.Count} قلم");
            var ok = (brs?.Count ?? 0) + (tgju?.Count ?? 0) > 0;
            return new BotConnectorTestResult(ok, string.Join(" · ", parts));
        }

        public async Task<PriceSnapshot> GetAsync(CancellationToken ct)
        {
            if (_cache is { } hit && DateTime.UtcNow - hit.At < Fresh)
                return hit.Snapshot;

            await Gate.WaitAsync(ct);
            try
            {
                if (_cache is { } again && DateTime.UtcNow - again.At < Fresh)
                    return again.Snapshot;

                var found = new Dictionary<string, PriceItem>();
                var sources = new List<string>();

                var brs = await TryBrsApiAsync(ct);
                if (brs is { Count: > 0 }) { Merge(found, brs); sources.Add("BrsApi"); }

                if (!found.ContainsKey("usd") || !found.ContainsKey("coin_emami"))
                {
                    var tgju = await TryTgjuAsync(ct);
                    if (tgju is { Count: > 0 }) { Merge(found, tgju); sources.Add("TGJU"); }
                }

                if (!found.ContainsKey("usdt") || !found.ContainsKey("btc"))
                {
                    var nobitex = await TryNobitexAsync(ct);
                    if (nobitex is { Count: > 0 }) { Merge(found, nobitex); sources.Add("نوبیتکس"); }
                }

                if (found.Count == 0)
                {
                    if (_cache is { } old && DateTime.UtcNow - old.At < MaxStale)
                        return old.Snapshot with { Stale = true };
                    return new PriceSnapshot(false, new List<PriceItem>(), string.Empty, string.Empty, false);
                }

                var items = Wanted.Where(w => found.ContainsKey(w.Key)).Select(w => found[w.Key] with { Title = w.Title }).ToList();
                var snapshot = new PriceSnapshot(true, items, AssistantFormat.Clock(IranTime.Now), string.Join("، ", sources), false);
                _cache = new CacheEntry(DateTime.UtcNow, snapshot);
                return snapshot;
            }
            finally
            {
                Gate.Release();
            }
        }

        private static void Merge(Dictionary<string, PriceItem> into, List<PriceItem> items)
        {
            foreach (var item in items)
                into.TryAdd(item.Key, item);
        }

        private string? BrsKey => configuration["Assistant:BrsApiKey"];

        // ------------------------------------------------------------ BrsApi.ir

        private async Task<List<PriceItem>?> TryBrsApiAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(BrsKey)) return null;
            try
            {
                using var json = await GetJsonAsync($"https://Api.BrsApi.ir/Market/Gold_Currency.php?key={Uri.EscapeDataString(BrsKey)}", ct);
                var list = new List<PriceItem>();
                foreach (var group in json.RootElement.EnumerateObject())
                {
                    if (group.Value.ValueKind != JsonValueKind.Array) continue;
                    foreach (var item in group.Value.EnumerateArray())
                    {
                        var symbol = Str(item, "symbol")?.ToUpperInvariant() ?? string.Empty;
                        var name = Normalize(Str(item, "name") ?? string.Empty);
                        var key = BrsKeyOf(symbol, name);
                        if (key is null || !TryNumber(item, "price", out var price) || price <= 0) continue;
                        var unit = Str(item, "unit") ?? string.Empty;
                        var toman = unit.Contains("ریال") ? price / 10 : price;
                        // تتر و بیت‌کوین در BrsApi گاهی دلاری‌اند؛ فقط قیمت تومانی قبول است.
                        if (unit.Contains("دلار") || unit.Contains("USD", StringComparison.OrdinalIgnoreCase)) continue;
                        list.Add(new PriceItem(key, key, (long)Math.Round(toman), TryNumber(item, "change_percent", out var cp) ? cp : null));
                    }
                }
                return list;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                logger.LogWarning(ex, "BrsApi price fetch failed");
                return null;
            }
        }

        private static string? BrsKeyOf(string symbol, string name) => symbol switch
        {
            "USD" => "usd", "EUR" => "eur", "AED" => "aed",
            "IR_GOLD_18K" => "gold18", "IR_GOLD_MELTED" => "mesghal",
            "IR_COIN_EMAMI" => "coin_emami", "IR_COIN_BAHAR" => "coin_bahar",
            "IR_COIN_HALF" => "coin_half", "IR_COIN_QUARTER" => "coin_quarter",
            "USDT" => "usdt", "BTC" => "btc",
            _ => name switch
            {
                "دلار" or "دلارامریکا" => "usd",
                "یورو" => "eur",
                "درهمامارات" or "درهم" => "aed",
                "طلای18عیار" or "طلا18عیار" or "طلای18عیار(گرم)" => "gold18",
                "مثقالطلا" or "طلایابشده(مثقال)" or "مثقال" => "mesghal",
                "سکهامامی" or "سکهجدید" => "coin_emami",
                "سکهبهارازادی" or "سکهقدیم" => "coin_bahar",
                "نیمسکه" => "coin_half",
                "ربعسکه" => "coin_quarter",
                "تتر" => "usdt",
                "بیتکوین" => "btc",
                _ => null,
            },
        };

        // ------------------------------------------------------------ TGJU

        private static readonly (string Tgju, string Key)[] TgjuKeys =
        {
            ("price_dollar_rl", "usd"), ("price_eur", "eur"), ("price_aed", "aed"),
            ("sekee", "coin_emami"), ("sekeb", "coin_bahar"), ("nim", "coin_half"), ("rob", "coin_quarter"),
            ("geram18", "gold18"), ("mesghal", "mesghal"),
        };

        private async Task<List<PriceItem>?> TryTgjuAsync(CancellationToken ct)
        {
            try
            {
                using var json = await GetJsonAsync("https://call1.tgju.org/ajax.json", ct);
                if (!json.RootElement.TryGetProperty("current", out var current)) return null;
                var list = new List<PriceItem>();
                foreach (var (tgju, key) in TgjuKeys)
                {
                    if (!current.TryGetProperty(tgju, out var item) || !TryNumber(item, "p", out var rial) || rial <= 0) continue;
                    double? change = TryNumber(item, "dp", out var dp) ? dp : null;
                    if (change is not null && Str(item, "dt") == "low") change = -Math.Abs(change.Value);
                    list.Add(new PriceItem(key, key, (long)Math.Round(rial / 10), change));
                }
                return list;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                logger.LogWarning(ex, "TGJU price fetch failed");
                return null;
            }
        }

        // ------------------------------------------------------------ نوبیتکس

        private async Task<List<PriceItem>?> TryNobitexAsync(CancellationToken ct)
        {
            try
            {
                using var json = await GetJsonAsync("https://api.nobitex.ir/market/stats?srcCurrency=usdt,btc&dstCurrency=rls", ct);
                if (!json.RootElement.TryGetProperty("stats", out var stats)) return null;
                var list = new List<PriceItem>();
                foreach (var (market, key) in new[] { ("usdt-rls", "usdt"), ("btc-rls", "btc") })
                {
                    if (!stats.TryGetProperty(market, out var item) || !TryNumber(item, "latest", out var rial) || rial <= 0) continue;
                    list.Add(new PriceItem(key, key, (long)Math.Round(rial / 10), TryNumber(item, "dayChange", out var dc) ? dc : null));
                }
                return list;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Nobitex price fetch failed");
                return null;
            }
        }

        // ------------------------------------------------------------ کمکی

        private static async Task<JsonDocument> GetJsonAsync(string url, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible; NaragramAssistant/1.0)");
            using var response = await Http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }

        private static string? Str(JsonElement item, string name)
            => item.TryGetProperty(name, out var v) ? (v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString()) : null;

        /// <summary>عدد یا رشته با جداکننده و ارقام فارسی.</summary>
        private static bool TryNumber(JsonElement item, string name, out double value)
        {
            value = 0;
            if (!item.TryGetProperty(name, out var v)) return false;
            if (v.ValueKind == JsonValueKind.Number) return v.TryGetDouble(out value);
            if (v.ValueKind != JsonValueKind.String) return false;
            var raw = BotText.ToLatinDigits(v.GetString()).Replace(",", "").Replace("٬", "").Replace("%", "").Trim();
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static string Normalize(string name)
            => new string(name.Replace('ي', 'ی').Replace('ك', 'ک').Replace('آ', 'ا')
                .Where(c => !char.IsWhiteSpace(c) && c != '‌').ToArray())
               .Replace("۱۸", "18");
    }
}
