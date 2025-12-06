using CleanArchitecture.Application.Channels.Command.ChannelMessage;
using CleanArchitecture.Application.Channels.Command.CreateChannel;
using CleanArchitecture.Application.Channels.Command.DeleteChannelMember;
using CleanArchitecture.Application.Channels.Command.JoinPublicChannel;
using CleanArchitecture.Application.Channels.Command.Members.AddMember;
using CleanArchitecture.Application.Channels.Command.ProcessFilesChannel;
using CleanArchitecture.Application.Channels.Command.PromoteOrDemotToAdmin;
using CleanArchitecture.Application.Channels.Command.RenameBio;
using CleanArchitecture.Application.Channels.Command.UploadChannelAvatar;
using CleanArchitecture.Application.Channels.Query;
using CleanArchitecture.Application.Channels.Query.AllPublicChannels;
using CleanArchitecture.Application.Channels.Query.ChannelAvatar;
using CleanArchitecture.Application.Channels.Query.ChannelMembers;
using CleanArchitecture.Application.Channels.Query.DownLoadFile;
using CleanArchitecture.Application.Channels.Query.FilesList;
using CleanArchitecture.Application.Chats.FileMessages.Command.CreateMessage;
using CleanArchitecture.Application.Chats.Messages;
using CleanArchitecture.Application.Chats.Messages.Command.CreatMessage;
using CleanArchitecture.Application.Users.Commands.UploadAvatar;
using CleanArchitecture.Application.Users.Queries.GetUserAvatar;
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
            app.MapGet("/publicChannels", async ([FromServices] ISender sender) =>
            {

                var result = await sender.Send(new PublicChannelsQuery());
                return Results.Ok(result);
            });
            app.MapGet("{channelid:guid}/ChannelMembers", async ([FromServices] ISender sender, [FromRoute]Guid channelid) =>
            {

                var result = await sender.Send(new ChannelMemberQuery(channelid));
                return Results.Ok(result);
            });
            app.MapPost("/JoinPublicChannel", async ([FromServices] ISender sender
, [FromBody] JoinPublicChannelCommand command) =>
            {

                var result = await sender.Send(command);
                return Results.Ok(result);
            });
            app.MapPost("/AddNewMember", async ([FromServices] ISender sender
, [FromBody] AddNewMemberCommand command) =>
            {

                var result = await sender.Send(command);
                return Results.Ok(result);
            });
            app.MapPost("/UploadFile", async (
     [FromServices] ISender mediator,
    [FromForm] ProcessFilesChannelCommand file,
CancellationToken cancellationToken) =>
            {
                if (file.file == null || file.file.Length == 0)
                    return Results.BadRequest("فایل ارسالی معتبر نیست.");

                var response = await mediator.Send(file, cancellationToken);
                return Results.Ok(response);
            }).RequireAuthorization()
.DisableAntiforgery();
            app.MapPost("/SetProfile", handler: async (
   [FromServices] ISender mediator,
[FromForm] UploadChannelAvatarCommand file,
   CancellationToken cancellationToken) =>
            {
                if (file.file == null || file.file.Length == 0)
                    return Results.BadRequest("فایل ارسالی معتبر نیست.");

                var response = await mediator.Send(file , cancellationToken);
                return Results.Ok(response);
            }).RequireAuthorization()
.DisableAntiforgery();
            app.MapGet("getavatar", async ([FromServices] ISender Mediatr,
[FromQuery] Guid id, CancellationToken cancellationToken) =>
            {
                var result = await Mediatr.Send(new GetChannelAvatarQuery(id), cancellationToken);
                return Results.Ok(result);
            }).RequireAuthorization();

            app.MapPost("/DeleteMemberFromChannel", async ([FromServices] ISender sender
, [FromBody] DeleteMemberCommand command) =>
            {

                var result = await sender.Send(command);
                return Results.Ok(result);
            });
            app.MapGet("getChannelFiles", async ([FromServices] ISender Mediatr,
[FromQuery] Guid id, CancellationToken cancellationToken) =>
            {
                var result = await Mediatr.Send(new ChannelFilesQuery(id), cancellationToken);
                return Results.Ok(result);
            }).RequireAuthorization();
            app.MapGet("DownloadChannelQuery", async ([FromServices] ISender Mediatr,
[FromQuery] Guid id, CancellationToken cancellationToken) =>
            {
                var result = await Mediatr.Send(new DownloadFileQuery(id), cancellationToken);
                return Results.Ok(result);
            }).RequireAuthorization();





        }
    }
}
