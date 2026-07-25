using Azure;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Users.Commands.CreateClaims;
using CleanArchitecture.Application.Users.Commands.CreateUser;
using CleanArchitecture.Application.Users.Commands.LoginOrRegister;
using CleanArchitecture.Application.Users.Commands.ModifiedUser;
using CleanArchitecture.Application.Users.Commands.SendValdateCode;
using CleanArchitecture.Application.Users.Commands.UploadAvatar;
using CleanArchitecture.Application.Users.Queries.ExportUser;
using CleanArchitecture.Application.Users.Queries.GetUser;
using CleanArchitecture.Application.Users.Queries.GetUserAvatar;
using CleanArchitecture.Application.Users.Queries.GetUserInfoQuery;
using CleanArchitecture.Application.Users.Queries.UserList;
using CleanArchitecture.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CleanArchitecture.Presentation.EndPoint.User;


public sealed class UsersEndpoint:IEndpoint
{


    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("GetAll", async ([FromServices] ISender Mediatr,
           [FromQuery] string? search, CancellationToken cancellationToken) =>
        {
            var result = await Mediatr.Send(new GetUserQuery(search), cancellationToken);
            return Results.Ok(result);
        });
        app.MapGet("GetUsersForGroup", async ([FromServices] ISender Mediatr,
        [FromQuery] string? search, CancellationToken cancellationToken) =>
        {
            var result = await Mediatr.Send(new GetUsersListQuery(search), cancellationToken);
            return Results.Ok(result);
        });

        app.MapGet("GetGetUserBy", async ([FromServices] ISender Mediatr,
 [FromQuery] Guid? Id, CancellationToken cancellationToken) =>
        {
            var result = await Mediatr.Send(new CurrentUserInfoQuery(Id), cancellationToken);
            return Results.Ok(result);
        }) .RequireAuthorization();


        app.MapGet("ExportUsers", async ([FromServices] ISender Mediatr, [FromQuery] string ? Search, CancellationToken cancellationToken) =>
        {
           var result = await Mediatr.Send(new ExportUserQuery(Search), cancellationToken);
            if(result.IsSucceded)
            return Results.File(result.result.Content,result.result.ContentType,result.result.FileName);
          return  Results.NotFound();
        });
        
        app.MapPost("register", async ([FromServices] ISender Mediatr, 
         [FromBody] CreateUserCommand UserRegisterDto, CancellationToken cancellationToken) =>
        {

            var response = await Mediatr.Send(UserRegisterDto, cancellationToken);

            return Results.Ok(response);
        }).AllowAnonymous();
        
        app.MapPost("register-or-login", async ([FromServices] ISender Mediatr,
 [FromBody] LoginOrRegisterCommand UserRegisterDto, CancellationToken cancellationToken) =>
        {

            var response = await Mediatr.Send(UserRegisterDto, cancellationToken);

            return Results.Ok(response);
        }).AllowAnonymous();
        app.MapPost("SendVerifyCode", async ([FromServices] ISender Mediatr,
[FromBody] SendValidateCodeCommand UserRegisterDto, CancellationToken cancellationToken) =>
        {

            var response = await Mediatr.Send(UserRegisterDto, cancellationToken);

            return Results.Ok(response);
        }).AllowAnonymous();

        app.MapPost("User/CreateClaims", async ([FromServices] ISender Mediatr,
[FromBody] CreateClaimsCommand UserRegisterDto, CancellationToken cancellationToken) =>
        {

            var response = await Mediatr.Send(UserRegisterDto, cancellationToken);
            return Results.Ok(response);
        });


        app.MapPost("User/SetProfile", async (
           [FromServices] ISender mediator,
       [FromForm]   UplaodAvatarCommand file,
    
           CancellationToken cancellationToken) =>
        {
            if (file.file == null || file.file.Length == 0)
                return Results.BadRequest("فایل ارسالی معتبر نیست.");

            var response = await mediator.Send(file, cancellationToken);
            return Results.Ok(response);
        }).RequireAuthorization()
        .DisableAntiforgery();


        app.MapPost("/ModifiedUser", async (ISender sender, ModifiedUserCommand command) =>
        {
           var result= await sender.Send(command);
            return Results.Ok(result);


        }).RequireAuthorization();
        app.MapGet("getavatar", async ([FromServices] ISender Mediatr,
[FromQuery] Guid id, CancellationToken cancellationToken) =>
        {
            var result = await Mediatr.Send(new GetUserAvatarQuery(id), cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization();
        

    }

}
