using CleanArchitecture.Presentation.EndPoint;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace CleanArchitecture.Presentation.Extensions;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        ServiceDescriptor[] serviceDescriptors = assembly
            .DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                           type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type))
            .ToArray();

        services.TryAddEnumerable(serviceDescriptors);

        return services;
    }

    public static IApplicationBuilder MapEndpoints(
        this WebApplication app,
        RouteGroupBuilder? routeGroupBuilder = null)
    {
        IEnumerable<IEndpoint> endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();

        IEndpointRouteBuilder builder = routeGroupBuilder is null ? app : routeGroupBuilder;
        var apiVersion = app.NewApiVersionSet()
              //.HasApiVersion(new Asp.Versioning.ApiVersion(2))
              .HasApiVersion(new Asp.Versioning.ApiVersion(1))
              .ReportApiVersions()
              .Build();

        // تعریف گروه اصلی بدون تگ
   

        foreach (IEndpoint endpoint in endpoints)
        {
            var className = endpoint.GetType().Name.ToLower().Replace("endpoint", "");
            var versionedGroupBase = app.MapGroup("/api/v{apiVersion:apiVersion}/"+className)
                               .WithApiVersionSet(apiVersion);

           

            var versionedGroup = versionedGroupBase.WithTags(className).RequireAuthorization();

            endpoint.MapEndpoint(versionedGroup);
        }

        return app;
    }

    public static RouteHandlerBuilder HasPermission(this RouteHandlerBuilder app, string permission)
    {
        var data= app.RequireAuthorization(permission);
        return data;
    }
}
