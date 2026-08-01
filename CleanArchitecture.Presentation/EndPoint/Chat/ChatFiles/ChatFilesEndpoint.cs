
using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Abstraction.Files;
using CleanArchitecture.Application.Chats.FileMessages.Query;
using CleanArchitecture.Application.Groups.Query.GetAvatar;
using CleanArchitecture.Application.Users.Queries.GetAvatar;
using CleanArchitecture.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace CleanArchitecture.Presentation.EndPoint.Chat.ChatFiles
{
    public class ChatFilesEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/{fileid:guid:required}/files", async (
     [FromServices] IChatFileStreamer streamer,
     [FromServices] IApplicationUserManager userManager,
     [FromRoute] Guid fileid,
     HttpContext http,
     CancellationToken ct) =>
            {
                var myId = userManager.UserId;
                if (myId is null) return Results.Unauthorized();

                var meta = await streamer.GetMetaAsync(fileid, myId.Value, ct);
                if (meta is null) return Results.NotFound();

                // هدرها را قبل از شروع نوشتن ست کن. Content-Length همان چیزی است که
                // نوار پیشرفت سمت مرورگر به آن نیاز دارد.
                http.Response.ContentType = meta.ContentType;
                http.Response.ContentLength = meta.Length;
                http.Response.Headers.ContentDisposition =
                    $"attachment; filename*=UTF-8''{Uri.EscapeDataString(meta.DownloadName)}";

                await streamer.StreamToAsync(fileid, http.Response.Body, ct);

                return Results.Empty;
            });
            //GroupAvatarQuery

            app.MapGet("/{fileid:guid:required}/{ischannel:bool}/getAvatar", async ([FromServices] ISender sender
     , [FromRoute] Guid fileid, [FromRoute] bool ischannel) =>
            {

                var result = await sender.Send(new AvatarIdQuery(fileid, ischannel));
                if (!result.IsSucceded )
                {
                    return Results.NotFound();
                }
                else
                {
                    return Results.File(result.result.Bytes, result.result.Name);
                    
                }

            }).AllowAnonymous();
            app.MapGet("/{fileid:guid:required}/getgroupAvatar", async ([FromServices] ISender sender
, [FromRoute] Guid fileid) =>
            {

                var result = await sender.Send(new GroupAvatarQuery(fileid));
                if (!result.IsSucceded)
                {
                    return Results.NotFound();
                }
                else
                {
                    return Results.File(result.result.Bytes, result.result.Name);

                }

            }).AllowAnonymous();
        }
    }
}
