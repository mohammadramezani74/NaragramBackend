using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CleanArchitecture.Application.Bots.Flows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Bots.Hr
{
    /// <summary>جواب یک فراخوانی INTS. اگر Ok نباشد Message متن فارسی آماده برای کاربر است.</summary>
    public sealed record IntsResponse(bool Ok, string? Error, string Message, JsonElement? Data)
    {
        public static IntsResponse Fail(string error, string message) => new(false, error, message, null);
    }

    public interface IIntsConnector : IBotConnector
    {
        /// <summary>
        /// یک متد ماژول ناراگرام INTS را «از طرف» کاربر صدا می‌زند. هیچ‌وقت استثنا
        /// نمی‌دهد؛ قطعی، کندی و خطاها به IntsResponse با پیام فارسی تبدیل می‌شوند.
        /// </summary>
        Task<IntsResponse> SendAsync(HttpMethod method, string action, string? phone, object? body, CancellationToken ct);
    }

    /// <summary>
    /// اتصال به ماژول ناراگرام INTS (api/v1/Naragram/Naragram/...).
    ///
    /// تنظیمات روی سرور ناراگرام (در گیت نمی‌رود؛ ریپو عمومی است):
    ///   "Ints": { "BaseUrl": "https://ints.example/", "ApiKey": "..." }
    ///
    /// هر درخواست سه سربرگ دارد: X-Api-Key (کلید)، X-On-Behalf-Of (موبایل کاربر، از حساب
    /// او در ناراگرام، نه از چیزی که تایپ کرده)، و برای ثبت‌ها X-Request-Id.
    /// </summary>
    public sealed class IntsConnector(IConfiguration configuration, ILogger<IntsConnector> logger) : IIntsConnector
    {
        private const string ModulePath = "api/v1/Naragram/Naragram/";

        private static readonly HttpClient Http = new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(10) })
        {
            Timeout = TimeSpan.FromSeconds(12),
        };

        public string Key => "ints";
        public string Title => "سامانه‌ی پرسنلی INTS";
        public string? Description => "فیش حقوقی، مشخصات پرسنلی و درخواست‌های پرسنلی کارمند از INTS. تنظیمات: Ints:BaseUrl و Ints:ApiKey روی سرور.";

        public async Task<BotConnectorTestResult> TestAsync(CancellationToken cancellationToken)
        {
            var r = await SendAsync(HttpMethod.Get, "Ping", phone: null, body: null, cancellationToken);
            return r.Ok
                ? new BotConnectorTestResult(true, "اتصال به INTS برقرار است.")
                : new BotConnectorTestResult(false, r.Error switch
                {
                    "not_configured" => "آدرس یا کلید INTS در تنظیمات سرور ناراگرام (Ints:BaseUrl، Ints:ApiKey) نیست.",
                    "invalid_key" => "INTS کلید را قبول نکرد؛ کلید دو طرف یکی نیست.",
                    "ip_not_allowed" => "INTS درخواست را از IP این سرور قبول نکرد؛ IP سرور ناراگرام را به NaragramIntegration:AllowedIps اضافه کنید.",
                    _ => r.Message,
                });
        }

        public async Task<IntsResponse> SendAsync(HttpMethod method, string action, string? phone, object? body, CancellationToken ct)
        {
            var baseUrl = configuration["Ints:BaseUrl"];
            var apiKey = configuration["Ints:ApiKey"];
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
                return IntsResponse.Fail("not_configured", "اتصال به سامانه‌ی پرسنلی هنوز راه‌اندازی نشده است.");

            string? mobile = null;
            if (phone is not null)
            {
                mobile = NormalizeMobile(phone);
                if (mobile is null)
                    return IntsResponse.Fail("no_phone", "شماره‌ی موبایل معتبری در حساب ناراگرام شما ثبت نیست.");
            }

            try
            {
                using var request = new HttpRequestMessage(method, new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), ModulePath + action));
                request.Headers.Add("X-Api-Key", apiKey);
                if (mobile is not null) request.Headers.Add("X-On-Behalf-Of", mobile);
                if (method != HttpMethod.Get) request.Headers.Add("X-Request-Id", Guid.NewGuid().ToString());
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (body is not null)
                    request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

                using var response = await Http.SendAsync(request, ct);
                var text = await response.Content.ReadAsStringAsync(ct);
                var json = TryParse(text);

                if (response.IsSuccessStatusCode)
                    return new IntsResponse(true, null, string.Empty, json);

                // خطاهای فیلتر امنیتی INTS: { ok: false, error, message }
                var error = json is { ValueKind: JsonValueKind.Object } e && e.TryGetProperty("error", out var er) ? er.GetString() : null;
                var message = json is { ValueKind: JsonValueKind.Object } m && m.TryGetProperty("message", out var ms) ? ms.GetString() : null;

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden && error is "invalid_key" or "ip_not_allowed")
                {
                    logger.LogError("INTS rejected Naragram: {Status} {Error}", (int)response.StatusCode, error);
                    return IntsResponse.Fail(error!, "سامانه‌ی پرسنلی فعلاً در دسترس نیست. لطفاً بعداً دوباره تلاش کنید.");
                }

                if ((int)response.StatusCode >= 500)
                {
                    logger.LogError("INTS {Action} failed: {Status} {Body}", action, (int)response.StatusCode, Truncate(text));
                    return IntsResponse.Fail("server_error", "سامانه‌ی پرسنلی با خطا روبه‌رو شد. لطفاً کمی بعد دوباره تلاش کنید.");
                }

                return IntsResponse.Fail(error ?? "error", message ?? "درخواست انجام نشد.");
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning("INTS {Action} timed out", action);
                return IntsResponse.Fail("timeout", "سامانه‌ی پرسنلی جواب نداد. لطفاً کمی بعد دوباره تلاش کنید.");
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "INTS {Action} unreachable", action);
                return IntsResponse.Fail("unreachable", "ارتباط با سامانه‌ی پرسنلی برقرار نشد. لطفاً کمی بعد دوباره تلاش کنید.");
            }
        }

        /// <summary>هر شکل موبایل ایران ← 09121234567؛ نامعتبر ← null.</summary>
        public static string? NormalizeMobile(string? input)
        {
            var d = new string(BotText.ToLatinDigits(input).Where(char.IsAsciiDigit).ToArray());
            if (d.StartsWith("0098")) d = d[4..];
            else if (d.StartsWith("98") && d.Length == 12) d = d[2..];
            if (d.Length == 10 && d[0] == '9') d = "0" + d;
            return d.Length == 11 && d.StartsWith("09") ? d : null;
        }

        private static JsonElement? TryParse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            try
            {
                using var doc = JsonDocument.Parse(text);
                return doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string Truncate(string s) => s.Length <= 500 ? s : s[..500];
    }
}
