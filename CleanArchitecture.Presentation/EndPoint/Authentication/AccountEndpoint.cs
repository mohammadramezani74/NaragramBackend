
using CleanArchitecture.Application.Authentication.Command.CreateRefreshToken;
using CleanArchitecture.Application.Authentication.Command.GoogleAuth;
using CleanArchitecture.Application.Authentication.Command.ProcessToken;
using CleanArchitecture.Application.Authentication.Command.RevokeToken;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.Authentication;

public class AccountEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        //269183400
        //GoogleAuthCommand
        app.MapPost("Login", async ([FromServices] ISender Mediatr,
               [FromBody] ProccessTokenCommand command,
                CancellationToken cancellationToken) =>
        {
      
            var result = await Mediatr.Send(command, cancellationToken);
            return Results.Ok(result);
        }).AllowAnonymous();

        app.MapPost("googleAuthentication", async ([FromServices] ISender Mediatr,
         [FromBody] GoogleAuthCommand command,
          CancellationToken cancellationToken) =>
        {

            var result = await Mediatr.Send(command, cancellationToken);
            return Results.Ok(result);
        }).AllowAnonymous();


        app.MapPost("LoginSwagger", async ([FromServices] ISender Mediatr,
          [FromServices] IHttpContextAccessor Context,
       [FromQuery] string? UserName, [FromQuery] string? password,
          CancellationToken cancellationToken) =>
        {
           
            UserName = UserName ?? Context.HttpContext!.Request.Form[nameof(UserName)].ToString();
            password = password ?? Context.HttpContext!.Request.Form[nameof(password)].ToString();

            var result = await Mediatr.Send(new ProccessTokenCommand(UserName, password), cancellationToken);
            return Results.Ok(new
            {
                access_token = result.result.token,
                refresh_token = result.result.refreshtoken,

            });
        }).AllowAnonymous();


        app.MapPost("RefreshToken", async ([FromServices] ISender Mediatr,
 [FromBody] CreateRefreshTokenCommand command, CancellationToken cancellationToken) =>
        {

            var result = await Mediatr.Send(command, cancellationToken);
            return Results.Ok(result);
        });

        app.MapPost("RevokeToken", async ([FromServices] ISender Mediatr,
[FromBody] RevokeTokenCommand command, CancellationToken cancellationToken) =>
        {

            var result = await Mediatr.Send(command, cancellationToken);
            return Results.Ok(result);
        });
    }
}
