using CleanArchitecture.Presentation.Extensions;
using CleanArchitecture.Application.Roles.Commands.AddRoleToUser;
using CleanArchitecture.Application.Roles.Commands.CreateRole;
using CleanArchitecture.Application.Roles.Commands.CreateRoleClaims;
using CleanArchitecture.Application.Roles.Commands.DeleteRole;
using CleanArchitecture.Application.Roles.Commands.DeleteRoleClaims;
using CleanArchitecture.Application.Roles.Commands.UpdateRole;
using CleanArchitecture.Application.Roles.Queries.GetClaims;
using CleanArchitecture.Application.Roles.Queries.GetRoles;
using CleanArchitecture.Application.Roles.Queries.GetUserRoles;
using CleanArchitecture.Application.Users.Commands.CreateClaims;
using CleanArchitecture.Application.Users.Commands.CreateUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.Roles
{
    public class RolesEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // مدیریت نقش‌ها فقط برای ادمین؛ قبلاً هر کاربر واردشده می‌توانست به خودش نقش بدهد.
            var admin = app.MapGroup(string.Empty).AdminOnly();
          
            admin.MapGet("/GetAll", async ([FromServices] ISender Mediatr,
                   [FromQuery] string? Search, CancellationToken cancellationToken) =>
                {
                    var result = await Mediatr.Send(new GetRolesQuery(Search), cancellationToken);
                    return Results.Ok(result);
                });
     


            admin.MapGet("{roleid:guid}/GetClaims", async ([FromServices] ISender Mediatr,
             [FromRoute] Guid roleid, CancellationToken cancellationToken) =>
                {
                    var result = await Mediatr.Send(new GetRoleClaimsQuery(roleid), cancellationToken);
                    return Results.Ok(result);
                });
            admin.MapGet("{userid:guid}/GetUserRoles", async ([FromServices] ISender Mediatr,
    [FromRoute] Guid userid, CancellationToken cancellationToken) =>
                {
                    var result = await Mediatr.Send(new GetUserRolesQuery(userid), cancellationToken);
                    return Results.Ok(result);
                });

            admin.MapPost("/AddUserToRole", async ([FromServices] ISender Mediatr,
             [FromBody] AddRoleToUserCommand addroletoUserCommand, CancellationToken cancellationToken) =>
                {

                    var response = await Mediatr.Send(addroletoUserCommand, cancellationToken);
                    return Results.Ok(response);
                });
            admin.MapPost("/CreateRole", async ([FromServices] ISender Mediatr,
       [FromBody] CreateRoleCommand addroletoUserCommand, CancellationToken cancellationToken) =>
                {

                    var response = await Mediatr.Send(addroletoUserCommand, cancellationToken);
                    return Results.Ok(response);
                });
            admin.MapPost("/CreateRoleClaims", async ([FromServices] ISender Mediatr,
    [FromBody] CreateRoleClaimsCommand addroletoUserCommand, CancellationToken cancellationToken) =>
                {

                    var response = await Mediatr.Send(addroletoUserCommand, cancellationToken);
                    return Results.Ok(response);
                });
            admin.MapPut("/UpdateRole", async ([FromServices] ISender Mediatr,
    [FromBody] UpdateRoleCommand command, CancellationToken cancellationToken) =>
                {

                    var response = await Mediatr.Send(command, cancellationToken);
                    return Results.Ok(response);
                });
            admin.MapDelete("/DeleteRole/{Name}", async ([FromServices] ISender Mediatr,
    [FromRoute] string Name, CancellationToken cancellationToken) =>
                {

                    var response = await Mediatr.Send(new DeleteRoleCommand(Name), cancellationToken);
                    return Results.Ok(response);
                });
            admin.MapDelete("/DeleteRoleClaim/", async ([FromServices] ISender Mediatr,
 Guid RoleId, string Name, CancellationToken cancellationToken) =>
            {

                var response = await Mediatr.Send(new DeleteRoleClaimsCommand(RoleId, Name), cancellationToken);
                return Results.Ok(response);
            });

        }
    }
}

