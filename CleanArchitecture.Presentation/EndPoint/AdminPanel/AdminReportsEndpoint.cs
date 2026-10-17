using CleanArchitecture.Application.AdminPanel;
using CleanArchitecture.Presentation.EndPoint.Bots;
using CleanArchitecture.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.AdminPanel
{
    /// <summary>گزارش‌های پنل ادمین. مسیر پایه: /api/v1/adminreports (فقط Admin:UserIds).</summary>
    public sealed class AdminReportsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var admin = app.MapGroup(string.Empty).AdminOnly();

            admin.MapGet("/usage", async ([FromServices] ISender sender, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetUsageReportQuery(from ?? DateTime.Today.AddDays(-29), to ?? DateTime.Today), ct)));

            admin.MapGet("/inactive", async ([FromServices] ISender sender, [FromQuery] int? days, [FromQuery] int? take, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetInactiveUsersQuery(days ?? 30, take ?? 200), ct)));

            admin.MapGet("/audit", async (
                [FromServices] ISender sender,
                [FromQuery] int? page, [FromQuery] int? size, [FromQuery] string? area, [FromQuery] Guid? actorId,
                [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? search,
                CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetAuditLogQuery(page ?? 1, size ?? 25, area, actorId, from, to, search), ct)));
        }
    }
}
