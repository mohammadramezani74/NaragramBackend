
using CleanArchitecture.Application.Chats.Conversations.Query;
using CleanArchitecture.Application.Chats.FileMessages.Command.CreateMessage;
using CleanArchitecture.Application.Chats.Messages;
using CleanArchitecture.Application.Chats.Messages.Command.CreatMessage;
using CleanArchitecture.Application.Chats.Messages.Command.DeleteMessage;
using CleanArchitecture.Application.Chats.Messages.Command.ModifiedMessage;
using CleanArchitecture.Application.Chats.Messages.Command.NewReaction;
using CleanArchitecture.Application.Users.Commands.UploadAvatar;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.Chat.Messages
{
    public sealed class MessageEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/{conversationid:guid:required}/messages", async ([FromServices] ISender sender
             , [FromRoute] Guid conversationid,
             [FromQuery] int messageCount) =>
            {

                var result = await sender.Send(new MessagesQuery(conversationid, messageCount));
                return Results.Ok(result);
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
        }
    }
}
