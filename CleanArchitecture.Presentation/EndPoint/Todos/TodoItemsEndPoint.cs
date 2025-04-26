
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.TodoItems.Command.Compelete;
using CleanArchitecture.Application.TodoItems.Command.Create;
using CleanArchitecture.Application.TodoItems.Command.Delete;
using CleanArchitecture.Application.TodoItems.Query.Get;
using CleanArchitecture.Application.TodoItems.Query.GetById;
using CleanArchitecture.Application.Users.Commands.CreateUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.Presentation.EndPoint.Todos
{
    public sealed class TodoItemsEndPoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("{id:guid}/complete", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new CompleteTodoCommand(id);

                var result = await sender.Send(command, cancellationToken);

          
                    return Results.Ok(result);
                
            
            }).RequireAuthorization();

            app.MapPost("todos", async ([FromServices] ISender Mediatr,
      [FromBody] CreateTodoCommand UserRegisterDto, CancellationToken cancellationToken) =>
            {

                var response = await Mediatr.Send(UserRegisterDto, cancellationToken);
                return Results.Ok(response);
            }).RequireAuthorization();

            app.MapGet("todos", async ([FromServices] ISender Mediatr,
   CancellationToken cancellationToken) =>
            {

                var response = await Mediatr.Send(new GetTodosQuery(), cancellationToken);
                return Results.Ok(response);
            }).RequireAuthorization();
            app.MapGet("{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new GetTodoByIdQuery(id);

               var result = await sender.Send(command, cancellationToken);

                return Results.Ok(result);
            })
       .RequireAuthorization();

            app.MapDelete("{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new DeleteTodoCommand(id);

                var result = await sender.Send(command, cancellationToken);

                return Results.Ok(result);
            }).RequireAuthorization();

        }
    }
}
