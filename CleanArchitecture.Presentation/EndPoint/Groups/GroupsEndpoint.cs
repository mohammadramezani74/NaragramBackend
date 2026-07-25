
using CleanArchitecture.Application.Channels.Command.DeleteChannelMember;
using CleanArchitecture.Application.Channels.Command.Members.AddMember;
using CleanArchitecture.Application.Channels.Command.PromoteOrDemotToAdmin;
using CleanArchitecture.Application.Channels.Command.RenameBio;
using CleanArchitecture.Application.Channels.Query.ChannelMembers;
using CleanArchitecture.Application.Channels.Query.DownLoadFile;
using CleanArchitecture.Application.Channels.Query.FilesList;
using CleanArchitecture.Application.Chats.Conversations.Command.CreateGroupConverSation;
using CleanArchitecture.Application.Chats.FileMessages.Command.CreateMessage;
using CleanArchitecture.Application.Chats.Messages;
using CleanArchitecture.Application.Groups.Command.AddNewMemberToGroup;
using CleanArchitecture.Application.Groups.Command.ChangeGroupBio;
using CleanArchitecture.Application.Groups.Command.MuteUser;
using CleanArchitecture.Application.Groups.Command.PromoteOrDemoteToAdmin;
using CleanArchitecture.Application.Groups.Command.RemoveMemberFromGroup;
using CleanArchitecture.Application.Groups.Command.SendFileMessage;
using CleanArchitecture.Application.Groups.Command.SendMessage;
using CleanArchitecture.Application.Groups.Command.UploadGroupAvtar;
using CleanArchitecture.Application.Groups.Query.DownloadFile;
using CleanArchitecture.Application.Groups.Query.FilesList;
using CleanArchitecture.Application.Groups.Query.GetMessages;
using CleanArchitecture.Application.Groups.Query.GroupMembers;
using CleanArchitecture.Application.Users.Commands.UploadAvatar;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.Groups
{
    public class GroupsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/CreateGroupConversation", async ([FromServices] ISender sender
        , [FromBody] CreateGroupConversationCommand command) =>
            {

                var result = await sender.Send(command);
                return Results.Ok(result);
            });
            app.MapPost("/MuteSelectedUser", async ([FromServices] ISender sender
   , [FromBody] MuteUserCommand command) =>
            {

                var result = await sender.Send(command);
                return Results.Ok(result);
            });
            app.MapPost("/SendMeesageToGroup", async ([FromServices] ISender sender
  , [FromBody] SendMeesageToGroupCommand command) =>
            {

                var result = await sender.Send(command);
                if (result.IsSucceded)
                {
                    return Results.Ok(result);
                }
                else
                {
                    return Results.BadRequest(result);
                }
               
            });
            app.MapGet("/{conversationid:guid:required}/messages", async ([FromServices] ISender sender
        , [FromRoute] Guid conversationid,
        [FromQuery] int messageCount) =>
            {

                var result = await sender.Send(new GroupMessagesQuery(conversationid, messageCount));
                return Results.Ok(result);
            });
            app.MapGet("{channelid:guid}/GroupMembers", async ([FromServices] ISender sender, [FromRoute] Guid channelid) =>
            {

                var result = await sender.Send(new GetGroupMembersQuery(channelid));
                return Results.Ok(result);
            });
            app.MapPost("/PromotOrDemoteUser", async ([FromServices] ISender sender
, [FromBody] ChangeUserGroupPolicyCommand command) =>
            {

                var result = await sender.Send(command);
                return Results.Ok(result);
            });
            app.MapPost("/DeleteMemberFromGroup", async ([FromServices] ISender sender
, [FromBody] RemoveMemberFromGroupCommand command) =>
            {

                var result = await sender.Send(command);
                return Results.Ok(result);
            });

            app.MapPost("/AddNewMember", async ([FromServices] ISender sender
, [FromBody] AddNewMemberToGroupCommand command) =>
            {

                var result = await sender.Send(command);
                return Results.Ok(result);
            });
            app.MapPost("/UploadFile", async (
                 [FromServices] ISender mediator,
                [FromForm] SendFileMessageCommand file,

   CancellationToken cancellationToken) =>
            {
                if (file.file == null || file.file.Length == 0)
                    return Results.BadRequest("فایل ارسالی معتبر نیست.");

                var response = await mediator.Send(file, cancellationToken);
                return Results.Ok(response);
            }).RequireAuthorization()
            .DisableAntiforgery();
            app.MapPost("/SetProfile", async (
     [FromServices] ISender mediator,
 [FromForm] UplaodGroupAvatarCommand file,

     CancellationToken cancellationToken) =>
            {
                if (file.file == null || file.file.Length == 0)
                    return Results.BadRequest("فایل ارسالی معتبر نیست.");

                var response = await mediator.Send(file, cancellationToken);
                return Results.Ok(response);
            }).RequireAuthorization()
              .DisableAntiforgery();
            app.MapGet("getGroupFiles", async ([FromServices] ISender Mediatr,
[FromQuery] Guid id, CancellationToken cancellationToken) =>
            {
                var result = await Mediatr.Send(new GroupFilesQuery(id), cancellationToken);
                return Results.Ok(result);
            }).RequireAuthorization();
            app.MapGet("DownloadGroupFile", async ([FromServices] ISender Mediatr,
[FromQuery] Guid id, CancellationToken cancellationToken) =>
            {
                var result = await Mediatr.Send(new DownloadGroupFileQuery(id), cancellationToken);
                return Results.Ok(result);
            }).RequireAuthorization();
            app.MapPost("/ChangeGroupBio", async ([FromServices] ISender sender
        , [FromBody] ChangeGroupBioCommand command) =>
            {

                var result = await sender.Send(command);
                return Results.Ok(result);
            });

        }
    }
}
