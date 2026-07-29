
using CleanArchitecture.Application.Channels.Command.DeleteChannel;
using CleanArchitecture.Application.Chats.Conversations.Query;
using CleanArchitecture.Application.Chats.FileMessages.Command.CreateMessage;
using CleanArchitecture.Application.Chats.Messages;
using CleanArchitecture.Application.Chats.Messages.Command.ClearHistory;
using CleanArchitecture.Application.Chats.Messages.Command.CreatMessage;
using CleanArchitecture.Application.Chats.Messages.Command.DeleteMessage;
using CleanArchitecture.Application.Chats.Messages.Command.ModifiedMessage;
using CleanArchitecture.Application.Chats.Messages.Command.NewReaction;
using CleanArchitecture.Application.Chats.Messages.Query.MessagesAround;
using CleanArchitecture.Application.Chats.Messages.Query.SearchMessages;
using CleanArchitecture.Application.Users.Commands.UploadAvatar;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.Chat.Messages
{
    public sealed class MessageEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // ---- بارگذاری پیام‌ها با cursor ----
            // اولین بار before را نفرست تا آخرین پیام‌ها بیاید.
            // برای اسکرول به بالا، NextCursor صفحه‌ی قبل را بفرست.
            app.MapGet("/{conversationId:guid:required}/messages", async (
      [FromServices] ISender sender,
      [FromRoute] Guid conversationId,
      [FromQuery] DateTime? before,
      [FromQuery] int take = 30,
      CancellationToken ct = default) =>
            {
                var result = await sender.Send(
                    new MessagesQuery(conversationId, before, take), ct);

                return result.IsSucceded
                    ? Results.Ok(result)
                    : Results.BadRequest(result.Message);
            });
            app.MapPost("/messages", async ([FromServices] ISender sender
         , [FromBody] CreateMessageCommand command ) =>
            {

                var result = await sender.Send(command);
                return Results.Ok(result);
            });
            app.MapPut("/EditMessage", async ([FromServices] ISender sender,
                [FromBody] ModifiedMessageCommand command) =>
            {
                var result = await sender.Send(command);
                return Results.Ok(result);
            });
            app.MapDelete("{id}/{otherid}/DeleteMessage", async ([FromServices] ISender sender,
             [FromRoute] Guid id,
             [FromRoute] Guid otherid) =>
            {
                var result = await sender.Send(new DeleteMessageCommand(id,otherid));
                return Results.Ok(result);
            });
            app.MapPost("/UploadFile", async (
                 [FromServices] ISender mediator,
                [FromForm] CreateFileMessageCommand file,

   CancellationToken cancellationToken) =>
            {
                if (file.file == null || file.file.Length == 0)
                    return Results.BadRequest("فایل ارسالی معتبر نیست.");

                var response = await mediator.Send(file, cancellationToken);
                return Results.Ok(response);
            }).RequireAuthorization()
            .DisableAntiforgery();
            app.MapPost("/newReactionOnMessage", async ([FromServices] ISender sender
      , [FromBody] MessageReactionCommand command) =>
            {

                var result = await sender.Send(command);
                return Results.Ok(result);
            }).RequireAuthorization();
            app.MapGet("/search", async (
               [FromServices] ISender sender,
               [FromQuery] string q,
               [FromQuery] Guid? conversationId,
               [FromQuery] Guid? channelId,
               [FromQuery] DateTime? before,
               [FromQuery] int take = 20,
               CancellationToken ct = default) =>
            {
                if (conversationId is null && channelId is null)
                    return Results.BadRequest("conversationId یا channelId لازم است.");

                var result = await sender.Send(
                    new SearchMessagesQuery(conversationId, channelId, q, before, take), ct);

                return result.IsSucceded
                    ? Results.Ok(result)
                    : Results.BadRequest(result.Message);
            });

            // ---- پرش به یک نتیجه به همراه متن اطرافش ----
            app.MapGet("/{messageId:guid}/around", async (
                [FromServices] ISender sender,
                [FromRoute] Guid messageId,
                [FromQuery] int take = 20,
                CancellationToken ct = default) =>
            {
                var result = await sender.Send(new MessagesAroundQuery(messageId, take), ct);

                return result.IsSucceded
                    ? Results.Ok(result)
                    : Results.BadRequest(result.Message);
            });

            app.MapDelete("/{conversationId:guid:required}/history", async (
    [FromServices] ISender sender,
    [FromRoute] Guid conversationId,
    CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new ClearConversationHistoryCommand(conversationId), ct);

                return result.IsSucceded
                    ? Results.Ok(result)
                    : Results.BadRequest(result.Message);
            });




          
        }
    }
}
