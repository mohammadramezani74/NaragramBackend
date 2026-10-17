using CleanArchitecture.Application.AdminPanel;
using CleanArchitecture.Presentation.EndPoint.Bots;
using CleanArchitecture.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.AdminPanel
{
    /// <summary>پیام همگانی از طریق ربات اطلاع‌رسانی. مسیر پایه: /api/v1/adminbroadcasts (فقط Admin:UserIds).</summary>
    public sealed class AdminBroadcastsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var admin = app.MapGroup(string.Empty).AdminOnly();

            admin.MapGet("/", async ([FromServices] ISender sender, [FromQuery] int? page, [FromQuery] int? size, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetBroadcastsQuery(page ?? 1, size ?? 10), ct)));

            admin.MapPost("/", async ([FromServices] ISender sender, [FromBody] CreateBroadcastCommand command, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(command, ct)));

            admin.MapPost("/preview", async ([FromServices] ISender sender, [FromBody] BroadcastAudience audience, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new PreviewBroadcastAudienceQuery(audience), ct)));

            admin.MapPost("/{id:guid}/cancel", async ([FromServices] ISender sender, [FromRoute] Guid id, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new CancelBroadcastCommand(id), ct)));
        }
    }
}
