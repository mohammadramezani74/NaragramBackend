
using CleanArchitecture.Application.Chats.FileMessages.Query;
using CleanArchitecture.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.Chat.ChatFiles
{
    public class ChatFilesEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/{fileid:guid:required}/files", async ([FromServices] ISender sender
      , [FromRoute] Guid fileid) =>
            {

                var result = await sender.Send(new GetTargetFileCommand(fileid));
                if (result.Status is 400) {
                return Results.BadRequest(result.Message);
                }
                if(result.result.Type is MessageType.Video )
                {
                    var stream = new MemoryStream(result.result.FileData);
                    stream.Position = 0;
                    return Results.File(stream, "application/octet-stream", result.result.Name, enableRangeProcessing: true);
                }
                else
                {if(result.result.thumbnail!=null)
                      return Results.Ok(new { data = Convert.ToBase64String(result.result.FileData),thumbnail= Convert.ToBase64String(result.result.thumbnail), contentType = "application/octet-stream", fileDownloadName = result.result.Name });
                      return Results.Ok(new { data = Convert.ToBase64String(result.result.FileData), contentType = "application/octet-stream", fileDownloadName = result.result.Name });

                }

            });
        }
    }
}
