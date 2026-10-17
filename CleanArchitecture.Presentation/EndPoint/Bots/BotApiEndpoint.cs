using CleanArchitecture.Application.Bots.Api;
using CleanArchitecture.Application.Bots.Models;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.Bots
{
    /// <summary>
    /// ارسال پیام از سیستم‌های بیرونی از طرف یک ربات (مثل Bot API تلگرام). مسیر پایه: /api/v1/botapi
    /// احراز هویت با کلید ربات در سربرگ X-Bot-Key است، نه توکن کاربر؛ برای همین AllowAnonymous.
    /// کلید هرگز در آدرس نمی‌آید تا در لاگ‌های IIS نماند.
    /// </summary>
    public sealed class BotApiEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // آزمایش کلید: کدام ربات؟ روشن است؟ رسمی است؟
            app.MapGet("/me", async (HttpContext http, [FromServices] IBotApiService api, CancellationToken ct) =>
                ToResult(await api.MeAsync(Key(http), ct)))
                .AllowAnonymous();

            app.MapPost("/send", async (
                HttpContext http,
                [FromServices] IBotApiService api,
                [FromBody] BotApiSendRequest? request,
                CancellationToken ct) =>
                ToResult(await api.SendAsync(Key(http), http.Connection.RemoteIpAddress?.ToString(), request, ct)))
                .AllowAnonymous();
        }

        private static string? Key(HttpContext http)
            => http.Request.Headers[BotApiKeys.HeaderName].FirstOrDefault();

        private static IResult ToResult(BotApiOutcome outcome)
            => Results.Json(outcome.Body, BotJson.Options, statusCode: outcome.StatusCode);
    }
}
