
using CleanArchitecture.Application.Chats.Conversations.Command.CreateGroupConverSation;
using CleanArchitecture.Application.Chats.Conversations.Command.CreatePrivateConversation;
using CleanArchitecture.Application.Chats.Conversations.Command.ProccessProfileImage;
using CleanArchitecture.Application.Chats.Conversations.Query;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.Chat.Conversations
{
    public sealed class ConversationEndPoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/MyConverSations", async ([FromServices] ISender sender
             , [AsParameters] MyConversationQuery query) =>
            {

                var result = await sender.Send(query);
                return Results.Ok(result);
            });

            app.MapPost("{id:guid}/CreateConversation", async ([FromServices] ISender sender
                , [FromRoute] Guid id) =>
            {

                var result = await sender.Send(new CreateConversationCommand(id));
                if (!result.IsSucceded)
                {
                    return Results.BadRequest(result);
                }
                return Results.Ok(result);
            });
            app.MapPost("/CreateGroupConversation", async ([FromServices] ISender sender
            , [FromBody] CreateGroupConversationCommand command) =>
            {

                var result = await sender.Send(command);
                return Results.Ok(result);
            });
            
            app.MapPost("{myid:guid}/{otherid}/ProcessImage", async ([FromServices] ISender sender
       , [FromRoute] Guid myid,
       [FromRoute] Guid? otherid
       ) =>
            {

                var result = await sender.Send(new ProfileImageCommand(myid,otherid));
                return Results.Ok(result);
            });
        }
    }
}
