
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
            app.MapGet("/{fileid:guid:required}/files", async ([FromServices] ISender sender
      , [FromRoute] Guid fileid, [FromQuery] bool raw = false) =>
            {

                var result = await sender.Send(new GetTargetFileCommand(fileid));
                if (result.Status is 400) return Results.BadRequest(result.Message);

                if (raw || result.result.Type is MessageType.Video)   // ← raw اضافه شد
                {
                    var stream = new MemoryStream(result.result.FileData, writable: false);
                    return Results.File(stream, "application/octet-stream",
                                        result.result.Name, enableRangeProcessing: true);
                }
                else
                {if(result.result.thumbnail!=null)
                      return Results.Ok(new { data = Convert.ToBase64String(result.result.FileData),thumbnail= Convert.ToBase64String(result.result.thumbnail), contentType = "application/octet-stream", fileDownloadName = result.result.Name });
                      return Results.Ok(new { data = Convert.ToBase64String(result.result.FileData), contentType = "application/octet-stream", fileDownloadName = result.result.Name });

                }

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
