using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using System.Reflection;
using CleanArchitecture.Application.Common.Behaviours;
using MediatR;
using Mapster;
using MapsterMapper;
using CleanArchitecture.Application.Chats.Hubs.Services;
using CleanArchitecture.Application.Bots;
using CleanArchitecture.Application.Bots.Engine;


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
            services.AddSingleton<CleanArchitecture.Application.Hubs.HubConnectionRegistry>();
            services.AddSingleton<CleanArchitecture.Application.Hubs.HubRoomMembership>();

            // ربات‌ها: صف، موتور، و جریان‌ها/اتصال‌های این اسمبلی (خودکار پیدا می‌شوند).
            services.AddSingleton<IBotUpdateQueue>(new BotUpdateQueue(partitions: 4));
            services.AddScoped<BotMessenger>();
            services.AddSingleton<CleanArchitecture.Application.Bots.Api.BotApiRateLimiter>();
            // ربات دستیار: تقویم بدون اینترنت و خلاصه‌ی صبحگاهی (آب‌وهوا و قیمت‌ها اتصال‌اند و خودکار ثبت می‌شوند)
            services.AddSingleton<CleanArchitecture.Application.Bots.Assistant.PersianCalendarService>();
            services.AddScoped<CleanArchitecture.Application.Bots.Assistant.IAssistantDigestService, CleanArchitecture.Application.Bots.Assistant.AssistantDigestService>();
            services.AddScoped<CleanArchitecture.Application.Bots.Api.IBotApiService, CleanArchitecture.Application.Bots.Api.BotApiService>();
            services.AddScoped<IBotEngine, BotEngine>();
            services.AddScoped<CleanArchitecture.Application.AdminPanel.IBroadcastSender, CleanArchitecture.Application.AdminPanel.BroadcastSender>();
            services.AddScoped<CleanArchitecture.Application.AdminPanel.IAppSettingsStore, CleanArchitecture.Application.AdminPanel.AppSettingsStore>();
            services.AddScoped<CleanArchitecture.Application.AdminPanel.ICreationPolicy, CleanArchitecture.Application.AdminPanel.CreationPolicy>();
            services.AddBotParts(Assembly.GetExecutingAssembly());

            return services;
        }
    }
}
