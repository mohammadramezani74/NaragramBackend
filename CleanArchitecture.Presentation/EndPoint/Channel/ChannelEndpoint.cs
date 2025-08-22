using CleanArchitecture.Application.Channels.Command.ChannelMessage;
using CleanArchitecture.Application.Channels.Command.CreateChannel;
using CleanArchitecture.Application.Channels.Command.PromoteOrDemotToAdmin;
using CleanArchitecture.Application.Channels.Command.RenameBio;
using CleanArchitecture.Application.Channels.Query;
using CleanArchitecture.Application.Chats.Messages;
using CleanArchitecture.Application.Chats.Messages.Command.CreatMessage;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.Channel
{
    public class ChannelEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/CreatenewChannel", async ([FromServices] ISender sender
                , [FromBody] CreateChannelCommand command) =>
            {

                var result = await sender.Send(command);
                if (!result.IsSucceded)
                {
                    return Results.BadRequest(result);
                }
                return Results.Ok(result);
            });
          
            app.MapPost("/ChangeDescription", async ([FromServices] ISender sender
              , [FromBody] ChangeBioChannelCommand command) =>
            {

                var result = await sender.Send(command);
                return Results.Ok(result);
            });
            app.MapPost("/PromotOrDemoteUser", async ([FromServices] ISender sender
        , [FromBody] ChangeUserChannelPolicyCommand command) =>
            {

                var result = await sender.Send(command);
                return Results.Ok(result);
            });
            app.MapGet("/{channelid:guid:required}/messages", async ([FromServices] ISender sender
         , [FromRoute] Guid channelid,
         [FromQuery] int messageCount) =>
            {

                var result = await sender.Send(new ChannelMessagesQuery(channelid, messageCount));
                return Results.Ok(result);
            });
            app.MapPost("/messages", async ([FromServices] ISender sender
    , [FromBody] SendChannelMessageCommand command) =>
            {

                var result = await sender.Send(command);
                return Results.Ok(result);
            });
        }
    }
}
