using CleanArchitecture.Application.Bots.Admin;
using CleanArchitecture.Application.Bots.Models;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.Bots
{
    /// <summary>
    /// پنل ادمین ربات‌ها. مسیر پایه: /api/v1/botadmin
    /// فقط برای حساب‌های Admin:UserIds؛ بقیه 404 می‌گیرند (هم این فیلتر، هم خود هندلرها چک می‌کنند).
    /// </summary>
    public sealed class BotAdminEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var admin = app.MapGroup(string.Empty).AdminOnly();

            admin.MapGet("/", async ([FromServices] ISender sender, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetBotsAdminQuery(), ct)));

            admin.MapGet("/{id:guid}", async ([FromServices] ISender sender, [FromRoute] Guid id, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetBotAdminQuery(id), ct)));

            admin.MapPost("/", async ([FromServices] ISender sender, [FromBody] CreateBotCommand command, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(command, ct)));

            admin.MapPut("/{id:guid}", async (
                [FromServices] ISender sender,
                [FromRoute] Guid id,
                [FromBody] UpdateBotBody body,
                CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new UpdateBotCommand(
                    id, body.Name, body.Description, body.IsEnabled, body.AccessMode, body.AccessList, body.Commands), ct)));

            admin.MapPut("/{id:guid}/menu", async (
                [FromServices] ISender sender,
                [FromRoute] Guid id,
                [FromBody] SaveMenuBody body,
                CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new SaveBotMenuCommand(id, body.Nodes), ct)));

            admin.MapGet("/flows", async ([FromServices] ISender sender, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetBotFlowsQuery(), ct)));

            admin.MapGet("/connectors", async ([FromServices] ISender sender, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetBotConnectorsQuery(), ct)));

            admin.MapPost("/connectors/{key}/test", async ([FromServices] ISender sender, [FromRoute] string key, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new TestBotConnectorCommand(key), ct)));

            // ارسال از بیرون: کلیدهای API، حالت رسمی و گزارش ارسال‌ها
            admin.MapGet("/{id:guid}/api", async ([FromServices] ISender sender, [FromRoute] Guid id, CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new GetBotApiQuery(id), ct)));

            admin.MapPost("/{id:guid}/api/keys", async (
                [FromServices] ISender sender,
                [FromRoute] Guid id,
                [FromBody] CreateKeyBody body,
                CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new CreateBotApiKeyCommand(id, body.Name), ct)));

            admin.MapDelete("/{id:guid}/api/keys/{keyId:guid}", async (
                [FromServices] ISender sender,
                [FromRoute] Guid id,
                [FromRoute] Guid keyId,
                CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new RevokeBotApiKeyCommand(id, keyId), ct)));

            admin.MapPut("/{id:guid}/api/official", async (
                [FromServices] ISender sender,
                [FromRoute] Guid id,
                [FromBody] OfficialBody body,
                CancellationToken ct) =>
                BotsEndpoint.ToResult(await sender.Send(new SetBotOfficialCommand(id, body.IsOfficial), ct)));
        }

        public sealed record UpdateBotBody(
            string Name,
            string? Description,
            bool IsEnabled,
            BotAccessMode AccessMode,
            List<Guid>? AccessList,
            List<BotCommandItem>? Commands);

        public sealed record SaveMenuBody(List<BotMenuNodeDto> Nodes);

        public sealed record CreateKeyBody(string Name);

        public sealed record OfficialBody(bool IsOfficial);
    }
}
