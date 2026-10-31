using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.AdminPanel;
using CleanArchitecture.Presentation.EndPoint.Bots;
using CleanArchitecture.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.AdminPanel
{
    /// <summary>ورود به پنل با کد پیامکی. مسیر پایه: /api/v1/adminauth</summary>
    public sealed class AdminAuthEndpoint : IEndpoint
    {
        public sealed record VerifyBody(string Code);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var signIn = app.MapGroup(string.Empty).AdminSignIn();

            signIn.MapPost("/code", async ([FromServices] ISender sender, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new SendAdminCodeCommand(), ct)));

            signIn.MapPost("/verify", async ([FromServices] ISender sender, [FromBody] VerifyBody body, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new VerifyAdminCodeCommand(body.Code), ct)));

            signIn.MapPost("/logout", async ([FromServices] ISender sender, HttpRequest request, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(
                    new AdminLogoutCommand(request.Headers[IAdminSessionService.HeaderName].ToString()), ct)));

            app.MapGet("/me", async ([FromServices] ISender sender, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetAdminMeQuery(), ct)))
                .AdminOnly();
        }
    }

    /// <summary>مدیران پنل و دسترسی‌هایشان؛ فقط مالک. مسیر پایه: /api/v1/adminmanagers</summary>
    public sealed class AdminManagersEndpoint : IEndpoint
    {
        public sealed record GrantBody(List<string> Permissions);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var owner = app.MapGroup(string.Empty).AdminOnly(PanelPermissions.Owner);

            owner.MapGet("/", async ([FromServices] ISender sender, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetAdminManagersQuery(), ct)));

            owner.MapGet("/candidates", async ([FromServices] ISender sender, [FromQuery] string? q, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new SearchAdminCandidatesQuery(q), ct)));

            owner.MapPut("/{userId:guid}", async ([FromServices] ISender sender, [FromRoute] Guid userId, [FromBody] GrantBody body, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new SaveAdminGrantCommand(userId, body.Permissions), ct)));

            owner.MapDelete("/{userId:guid}", async ([FromServices] ISender sender, [FromRoute] Guid userId, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new RemoveAdminGrantCommand(userId), ct)));

            owner.MapPost("/{userId:guid}/signout", async ([FromServices] ISender sender, [FromRoute] Guid userId, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new SignOutAdminCommand(userId), ct)));
        }
    }
}
