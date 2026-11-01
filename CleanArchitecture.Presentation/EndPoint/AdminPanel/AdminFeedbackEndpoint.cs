using CleanArchitecture.Application.AdminPanel;
using CleanArchitecture.Presentation.EndPoint.Bots;
using CleanArchitecture.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.AdminPanel
{
    /// <summary>نظر و پیشنهاد کاربران درباره‌ی اپ. مسیر پایه: /api/v1/adminfeedback</summary>
    public sealed class AdminFeedbackEndpoint : IEndpoint
    {
        public sealed record StatusBody(string Status, bool Notify);
        public sealed record ReplyBody(string Text, string? Status);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var admin = app.MapGroup(string.Empty).AdminOnly(PanelPermissions.Feedback);

            admin.MapGet("/", async (
                [FromServices] ISender sender,
                [FromQuery] int? page, [FromQuery] int? size, [FromQuery] string? status, [FromQuery] string? kind, [FromQuery] string? search,
                CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetFeedbackQuery(page ?? 1, size ?? 25, status, kind, search), ct)));

            admin.MapGet("/{id:guid}", async ([FromServices] ISender sender, [FromRoute] Guid id, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetFeedbackItemQuery(id), ct)));

            admin.MapPut("/{id:guid}/status", async ([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] StatusBody body, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new SetFeedbackStatusCommand(id, body.Status, body.Notify), ct)));

            admin.MapPost("/{id:guid}/reply", async ([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] ReplyBody body, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new ReplyFeedbackCommand(id, body.Text, body.Status), ct)));
        }
    }
}
