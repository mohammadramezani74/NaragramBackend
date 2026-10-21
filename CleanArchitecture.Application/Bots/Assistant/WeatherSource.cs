using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using CleanArchitecture.Application.Bots.Flows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Bots.Assistant
{
    public sealed record WeatherDay(string Label, double Min, double Max, string Text, string Emoji);

    public sealed record WeatherReport(
        string City,
        bool Ok,
        double Temp,
        string Text,
        string Emoji,
        int Humidity,
        int WindKmh,
        List<WeatherDay> Days,
        string UpdatedAt,
        bool Stale);

    public interface IWeatherSource : IBotConnector
    {
        /// <summary>هیچ‌وقت استثنا نمی‌دهد؛ اگر در دسترس نبود Ok = false.</summary>
        Task<WeatherReport> GetAsync(AssistantCity city, CancellationToken ct);
    }

    /// <summary>
    /// آب‌وهوا از MET Norway (api.met.no): رایگان، بدون کلید، استفاده‌ی تجاری مجاز با ذکر منبع
    /// (CC BY 4.0). شرطش User-Agent شناسنامه‌دار است: Assistant:WeatherUserAgent روی سرور.
    /// هر شهر نیم ساعت کش می‌شود؛ اگر اینترنت بین‌الملل قطع شد تا ۱۲ ساعت آخرین داده با برچسب
    /// «به‌روز نیست» نشان داده می‌شود.
    /// </summary>
    public sealed class MetNoWeatherSource(IConfiguration configuration, ILogger<MetNoWeatherSource> logger) : IWeatherSource
    {
        private static readonly HttpClient Http = new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(10) })
        {
            Timeout = TimeSpan.FromSeconds(10),
        };

        private static readonly ConcurrentDictionary<string, (DateTime At, WeatherReport Report)> Cache = new();
        private static readonly TimeSpan Fresh = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan MaxStale = TimeSpan.FromHours(12);

        public string Key => "assistant.weather";
        public string Title => "آب‌وهوا (MET Norway)";
        public string? Description => "پیش‌بینی هوای شهرها؛ رایگان و بدون کلید. منبع خارجی است و با قطع اینترنت بین‌الملل آخرین داده نشان داده می‌شود.";

        public async Task<BotConnectorTestResult> TestAsync(CancellationToken cancellationToken)
        {
            var r = await GetAsync(AssistantCities.Find("تهران")!, cancellationToken);
            return r.Ok && !r.Stale
                ? new BotConnectorTestResult(true, $"تهران الان {AssistantFormat.Degrees(r.Temp)}، {r.Text}")
                : new BotConnectorTestResult(false, r.Ok ? "فقط داده‌ی قدیمی در کش هست؛ سرویس جواب نداد." : "سرویس آب‌وهوا جواب نداد.");
        }

        public async Task<WeatherReport> GetAsync(AssistantCity city, CancellationToken ct)
        {
            if (Cache.TryGetValue(city.Name, out var hit) && DateTime.UtcNow - hit.At < Fresh)
                return hit.Report;

            try
            {
                var lat = city.Lat.ToString("0.####", CultureInfo.InvariantCulture);
                var lon = city.Lng.ToString("0.####", CultureInfo.InvariantCulture);
                using var request = new HttpRequestMessage(HttpMethod.Get,
                    $"https://api.met.no/weatherapi/locationforecast/2.0/compact?lat={lat}&lon={lon}");
                request.Headers.UserAgent.ParseAdd(configuration["Assistant:WeatherUserAgent"] ?? "NaragramAssistant/1.0 (internal company messenger)");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var response = await Http.SendAsync(request, ct);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

                var report = Parse(city, json.RootElement);
                Cache[city.Name] = (DateTime.UtcNow, report);
                return report;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Weather fetch failed for {City}", city.Name);
                if (Cache.TryGetValue(city.Name, out var old) && DateTime.UtcNow - old.At < MaxStale)
                    return old.Report with { Stale = true };
                return new WeatherReport(city.Name, false, 0, string.Empty, string.Empty, 0, 0, new List<WeatherDay>(), string.Empty, false);
            }
        }

        private static WeatherReport Parse(AssistantCity city, JsonElement root)
        {
            var series = root.GetProperty("properties").GetProperty("timeseries");
            var points = new List<(DateTime Local, double Temp, int Humidity, double Wind, string? Symbol6, string? Symbol1)>();
            foreach (var item in series.EnumerateArray())
            {
                var time = item.GetProperty("time").GetDateTime();
                var data = item.GetProperty("data");
                var details = data.GetProperty("instant").GetProperty("details");
                points.Add((
                    IranTime.FromUtc(time.ToUniversalTime()),
                    details.TryGetProperty("air_temperature", out var t) ? t.GetDouble() : double.NaN,
                    details.TryGetProperty("relative_humidity", out var h) ? (int)Math.Round(h.GetDouble()) : 0,
                    details.TryGetProperty("wind_speed", out var w) ? w.GetDouble() : 0,
                    Symbol(data, "next_6_hours") ?? Symbol(data, "next_12_hours"),
                    Symbol(data, "next_1_hours")));
            }

            if (points.Count == 0)
                throw new InvalidOperationException("Empty forecast");

            var now = points[0];
            var (nowText, nowEmoji) = Describe(now.Symbol1 ?? now.Symbol6);

            var today = IranTime.Today;
            var days = new List<WeatherDay>();
            for (var i = 0; i < 3; i++)
            {
                var day = today.AddDays(i);
                var list = points.Where(p => DateOnly.FromDateTime(p.Local) == day && !double.IsNaN(p.Temp)).ToList();
                if (list.Count == 0) continue;
                var noon = list.OrderBy(p => Math.Abs((p.Local.TimeOfDay - TimeSpan.FromHours(12)).TotalMinutes))
                               .FirstOrDefault(p => p.Symbol6 is not null || p.Symbol1 is not null);
                var (text, emoji) = Describe(noon.Symbol6 ?? noon.Symbol1);
                var label = i switch { 0 => "امروز", 1 => "فردا", _ => "پس‌فردا" };
                days.Add(new WeatherDay(label, list.Min(p => p.Temp), list.Max(p => p.Temp), text, emoji));
            }

            return new WeatherReport(
                city.Name, true, now.Temp, nowText, nowEmoji, now.Humidity, (int)Math.Round(now.Wind * 3.6),
                days, AssistantFormat.Clock(IranTime.Now), false);
        }

        private static string? Symbol(JsonElement data, string block)
            => data.TryGetProperty(block, out var b) && b.TryGetProperty("summary", out var s)
               && s.TryGetProperty("symbol_code", out var code) ? code.GetString() : null;

        /// <summary>symbol_code های MET Norway (مثل partlycloudy_day یا lightrainshowers_night) به فارسی.</summary>
        public static (string Text, string Emoji) Describe(string? symbol)
        {
            if (string.IsNullOrEmpty(symbol)) return ("نامشخص", "🌡");
            var night = symbol.EndsWith("_night", StringComparison.Ordinal);
            var s = symbol.Split('_')[0];
            if (s.Contains("thunder")) return ("رعد و برق", "⛈");
            if (s.Contains("sleet")) return ("برف و باران", "🌨");
            if (s.Contains("snow")) return (s.StartsWith("heavy") ? "برف سنگین" : s.StartsWith("light") ? "برف سبک" : "برف", "❄️");
            if (s.Contains("rain"))
            {
                var showers = s.Contains("showers");
                var text = s.StartsWith("heavy") ? (showers ? "رگبار شدید" : "باران شدید")
                         : s.StartsWith("light") ? (showers ? "رگبار پراکنده" : "باران سبک")
                         : showers ? "رگبار" : "باران";
                return (text, "🌧");
            }
            return s switch
            {
                "fog" => ("مه", "🌫"),
                "cloudy" => ("ابری", "☁️"),
                "partlycloudy" => ("نیمه‌ابری", night ? "☁️" : "⛅"),
                "fair" => ("کمی ابری", night ? "🌙" : "🌤"),
                "clearsky" => ("صاف", night ? "🌙" : "☀️"),
                _ => ("نامشخص", "🌡"),
            };
        }
    }
}
