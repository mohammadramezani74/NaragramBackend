using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Bots.Commands;
using CleanArchitecture.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.Bots
{
    /// <summary>
    /// ربات‌ها از دید کاربر. مسیر پایه: /api/v1/bots
    /// پیام متنی به ربات از همان مسیر پیام خصوصی (/api/v1/message/messages) می‌رود.
    /// </summary>
    public sealed class BotsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // فرانت فقط وقتی لینک پنل ادمین را نشان می‌دهد که این true باشد.
            app.MapGet("/admin-access", ([FromServices] IAdminAccess admin) =>
                Results.Ok(new { isAdmin = admin.CurrentUserIsAdmin }));

            app.MapGet("/{botUserId:guid}", async (
                [FromServices] ISender sender,
                [FromRoute] Guid botUserId,
                CancellationToken ct) =>
                ToResult(await sender.Send(new GetBotInfoQuery(botUserId), ct)));

            app.MapPost("/{botUserId:guid}/start", async (
                [FromServices] ISender sender,
                [FromRoute] Guid botUserId,
                CancellationToken ct) =>
                ToResult(await sender.Send(new StartBotCommand(botUserId), ct)));

            app.MapPost("/callback", async (
                [FromServices] ISender sender,
                [FromBody] BotCallbackCommand command,
                CancellationToken ct) =>
                ToResult(await sender.Send(command, ct)));
        }

        internal static IResult ToResult(OperationResult result) => result.IsSucceded
            ? Results.Ok(result)
            : result.Status switch
            {
                403 => Results.Json(result, statusCode: StatusCodes.Status403Forbidden),
                404 => Results.NotFound(result),
                _ => Results.BadRequest(result),
            };
    }
}
