using CleanArchitecture.Application.AdminPanel;
using CleanArchitecture.Presentation.EndPoint.Bots;
using CleanArchitecture.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.AdminPanel
{
    /// <summary>تنظیمات پنل ادمین. مسیر پایه: /api/v1/adminsettings (فقط Admin:UserIds).</summary>
    public sealed class AdminSettingsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var admin = app.MapGroup(string.Empty).AdminOnly();

            admin.MapGet("/", async ([FromServices] ISender sender, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetAdminSettingsQuery(), ct)));

            admin.MapPut("/announcement", async ([FromServices] ISender sender, [FromBody] AnnouncementSetting body, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new SaveAnnouncementCommand(body), ct)));

            admin.MapPut("/creation", async ([FromServices] ISender sender, [FromBody] CreationSetting body, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new SaveCreationCommand(body), ct)));
        }
    }

    /// <summary>اطلاعات عمومی برنامه برای هر کاربر واردشده: اعلان فعال و اجازه‌ی ساخت. مسیر: /api/v1/appinfo</summary>
    public sealed class AppInfoEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/", async ([FromServices] ISender sender, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetAppInfoQuery(), ct)));
        }
    }
}
