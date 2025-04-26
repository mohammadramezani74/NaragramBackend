using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using System.Reflection;
using CleanArchitecture.Application.Common.Behaviours;
using MediatR;
using Mapster;
using MapsterMapper;
using CleanArchitecture.Application.Chats.Hubs.Services;


namespace CleanArchitecture.Application
{
    public static class ConfigureServices
    {
        public static IServiceCollection RegisterApplicationServices(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehaviour<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
            });
            var config = TypeAdapterConfig.GlobalSettings;
            config.Scan(Assembly.GetExecutingAssembly());
          services.AddSingleton(config);
            services.AddScoped<IMapper,ServiceMapper>();
            services.AddSingleton<UserOnlineService>();

            return services;
        }
    }
}
