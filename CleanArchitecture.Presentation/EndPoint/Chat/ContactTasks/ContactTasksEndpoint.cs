using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.ContactTasks.Commands;
using CleanArchitecture.Application.ContactTasks.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.Chat.ContactTasks
{
    /// <summary>
    /// کارهای شخصی کاربر درباره‌ی هر مخاطب. مسیر پایه: /api/v1/contacttasks
    /// همه‌ی مسیرها فقط روی کارهای خودِ کاربرِ واردشده کار می‌کنند.
    /// </summary>
    public sealed class ContactTasksEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // تعداد کارهای باز به ازای هر مخاطب (برای ستون چت‌ها)
            app.MapGet("/counts", async (
                [FromServices] ISender sender,
                CancellationToken ct) =>
                ToResult(await sender.Send(new GetContactTaskCountsQuery(), ct)));

            // فهرست کارهای یک مخاطب: باز + آخرین انجام‌شده‌ها
            app.MapGet("/{contactId:guid}", async (
                [FromServices] ISender sender,
                [FromRoute] Guid contactId,
                CancellationToken ct) =>
                ToResult(await sender.Send(new GetContactTasksQuery(contactId), ct)));

            app.MapPost("/create", async (
                [FromServices] ISender sender,
                [FromBody] CreateContactTaskCommand command,
                CancellationToken ct) =>
                ToResult(await sender.Send(command, ct)));

            app.MapPut("/{id:guid}", async (
                [FromServices] ISender sender,
                [FromRoute] Guid id,
                [FromBody] UpdateContactTaskBody body,
                CancellationToken ct) =>
                ToResult(await sender.Send(new UpdateContactTaskCommand(id, body.Text, body.DueDate, body.RemindAt), ct)));

            app.MapPost("/{id:guid}/done", async (
                [FromServices] ISender sender,
                [FromRoute] Guid id,
                [FromQuery] bool value = true,
                CancellationToken ct = default) =>
                ToResult(await sender.Send(new SetContactTaskDoneCommand(id, value), ct)));

            app.MapDelete("/{id:guid}", async (
                [FromServices] ISender sender,
                [FromRoute] Guid id,
                CancellationToken ct) =>
                ToResult(await sender.Send(new DeleteContactTaskCommand(id), ct)));

            app.MapDelete("/{contactId:guid}/done", async (
                [FromServices] ISender sender,
                [FromRoute] Guid contactId,
                CancellationToken ct) =>
                ToResult(await sender.Send(new ClearDoneContactTasksCommand(contactId), ct)));

            app.MapPost("/reorder", async (
                [FromServices] ISender sender,
                [FromBody] ReorderContactTasksCommand command,
                CancellationToken ct) =>
                ToResult(await sender.Send(command, ct)));

            // انتقال یک‌باره‌ی چک‌لیست قدیمیِ مرورگر به سرور
            app.MapPost("/import", async (
                [FromServices] ISender sender,
                [FromBody] ImportContactTasksCommand command,
                CancellationToken ct) =>
                ToResult(await sender.Send(command, ct)));
        }

        private static IResult ToResult(OperationResult result) => result.IsSucceded
            ? Results.Ok(result)
            : result.Status switch
            {
                403 => Results.Json(result, statusCode: StatusCodes.Status403Forbidden),
                404 => Results.NotFound(result),
                _ => Results.BadRequest(result),
            };

        public sealed record UpdateContactTaskBody(string Text, DateTime? DueDate, DateTimeOffset? RemindAt = null);
    }
}
