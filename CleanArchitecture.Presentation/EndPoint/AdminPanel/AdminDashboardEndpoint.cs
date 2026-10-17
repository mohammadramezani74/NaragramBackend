using CleanArchitecture.Application.AdminPanel;
using CleanArchitecture.Presentation.EndPoint.Bots;
using CleanArchitecture.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.AdminPanel
{
    /// <summary>
    /// داشبورد پنل ادمین. مسیر پایه: /api/v1/admindashboard
    /// فقط برای حساب‌های Admin:UserIds؛ بقیه 404 می‌گیرند.
    /// </summary>
    public sealed class AdminDashboardEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var admin = app.MapGroup(string.Empty).AdminOnly();

            admin.MapGet("/", async ([FromServices] ISender sender, [FromQuery] int? days, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetAdminDashboardQuery(days ?? 7), ct)));
        }
    }
}
