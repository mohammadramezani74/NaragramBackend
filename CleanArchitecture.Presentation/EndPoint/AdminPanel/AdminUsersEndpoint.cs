using CleanArchitecture.Application.AdminPanel;
using CleanArchitecture.Presentation.EndPoint.Bots;
using CleanArchitecture.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.AdminPanel
{
    /// <summary>کاربران در پنل ادمین. مسیر پایه: /api/v1/adminusers (فقط Admin:UserIds).</summary>
    public sealed class AdminUsersEndpoint : IEndpoint
    {
        public sealed record AccessBody(string Action, string? Reason);
        public sealed record RolesBody(List<Guid> RoleIds);
        public sealed record NameBody(string FirstName, string LastName);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var admin = app.MapGroup(string.Empty).AdminOnly();

            admin.MapGet("/", async (
                [FromServices] ISender sender,
                [FromQuery] int? page, [FromQuery] int? size, [FromQuery] string? search,
                [FromQuery] string? status, [FromQuery] string? city, [FromQuery] Guid? roleId,
                CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetAdminUsersQuery(page ?? 1, size ?? 25, search, status, city, roleId), ct)));

            admin.MapGet("/{id:guid}", async ([FromServices] ISender sender, [FromRoute] Guid id, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetAdminUserQuery(id), ct)));

            admin.MapPost("/{id:guid}/phone", async ([FromServices] ISender sender, [FromRoute] Guid id, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new RevealUserPhoneCommand(id), ct)));

            admin.MapPost("/{id:guid}/access", async ([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] AccessBody body, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new SetUserAccessCommand(id, body.Action, body.Reason), ct)));

            admin.MapPut("/{id:guid}/name", async ([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] NameBody body, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new RenameUserCommand(id, body.FirstName, body.LastName), ct)));

            admin.MapPut("/{id:guid}/roles", async ([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] RolesBody body, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new SetUserRolesCommand(id, body.RoleIds), ct)));
        }
    }

    /// <summary>نقش‌ها در پنل ادمین. مسیر پایه: /api/v1/adminroles</summary>
    public sealed class AdminRolesEndpoint : IEndpoint
    {
        public sealed record RoleBody(string? Name);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var admin = app.MapGroup(string.Empty).AdminOnly();

            admin.MapGet("/", async ([FromServices] ISender sender, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetAdminRolesQuery(), ct)));

            admin.MapPost("/", async ([FromServices] ISender sender, [FromBody] RoleBody body, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new ManageRoleCommand("create", null, body.Name), ct)));

            admin.MapPut("/{id:guid}", async ([FromServices] ISender sender, [FromRoute] Guid id, [FromBody] RoleBody body, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new ManageRoleCommand("rename", id, body.Name), ct)));

            admin.MapDelete("/{id:guid}", async ([FromServices] ISender sender, [FromRoute] Guid id, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new ManageRoleCommand("delete", id, null), ct)));
        }
    }

    /// <summary>گروه‌ها و کانال‌ها در پنل ادمین. مسیر پایه: /api/v1/adminrooms/{group|channel}</summary>
    public sealed class AdminRoomsEndpoint : IEndpoint
    {
        public sealed record RoomActionBody(string Action, Guid? MemberId, string? Reason);
        public sealed record RoomEditBody(string Title, string? Description);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var admin = app.MapGroup(string.Empty).AdminOnly();

            admin.MapGet("/{kind}", async (
                [FromServices] ISender sender, [FromRoute] string kind,
                [FromQuery] int? page, [FromQuery] int? size, [FromQuery] string? search, [FromQuery] string? status,
                CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetAdminRoomsQuery(kind, page ?? 1, size ?? 25, search, status), ct)));

            admin.MapGet("/{kind}/{id:guid}", async ([FromServices] ISender sender, [FromRoute] string kind, [FromRoute] Guid id, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetAdminRoomQuery(kind, id), ct)));

            admin.MapPut("/{kind}/{id:guid}", async (
                [FromServices] ISender sender, [FromRoute] string kind, [FromRoute] Guid id,
                [FromBody] RoomEditBody body, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new EditRoomCommand(kind, id, body.Title, body.Description), ct)));

            admin.MapPost("/{kind}/{id:guid}", async (
                [FromServices] ISender sender, [FromRoute] string kind, [FromRoute] Guid id,
                [FromBody] RoomActionBody body, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new ManageRoomCommand(kind, id, body.Action, body.MemberId, body.Reason), ct)));
        }
    }
}
