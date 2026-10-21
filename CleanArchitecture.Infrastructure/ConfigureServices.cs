using CleanArchitecture.Application.Abstraction;
using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Abstraction.Caching;
using CleanArchitecture.Application.Abstraction.CsvFiles;
using CleanArchitecture.Application.Abstraction.Files;
using CleanArchitecture.Application.Abstraction.Purge;
using CleanArchitecture.Application.Abstraction.Sms;
using CleanArchitecture.Application.Abstraction.Storage;
using CleanArchitecture.Application.Abstraction.Uploader;
using CleanArchitecture.Application.Bots;
using CleanArchitecture.Infrastructure.Authentication;
using CleanArchitecture.Infrastructure.BackgroundJob;
using CleanArchitecture.Infrastructure.Caching;
using CleanArchitecture.Infrastructure.Files;
using CleanArchitecture.Infrastructure.HealthChecks;
using CleanArchitecture.Infrastructure.Notification;
using CleanArchitecture.Infrastructure.Persistence;
using CleanArchitecture.Infrastructure.Purge;
using CleanArchitecture.Infrastructure.smsProvider;
using CleanArchitecture.Infrastructure.Storage;
using CleanArchitecture.Infrastructure.Time;
using CleanArchitecture.Infrastructure.Uploader;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure;

public static class ConfigureServices
{
    public static IServiceCollection RegisterInfraStructureServices(this IServiceCollection services,IConfiguration configuration)
    {
        services.AddQuartz(confige =>
        {
            var job = new JobKey(nameof(ProcessOutBoxMessagesJob));
            confige.AddJob<ProcessOutBoxMessagesJob>(job)
            .AddTrigger(t => t.ForJob(job)
            .WithSimpleSchedule(s => s.WithIntervalInSeconds(10).RepeatForever()));

            // یادآوری کارهای شخصی؛ هر ۲۰ ثانیه یادآوری‌های رسیده را می‌فرستد.
            var reminders = new JobKey(nameof(SendContactTaskRemindersJob));
            confige.AddJob<SendContactTaskRemindersJob>(reminders)
            .AddTrigger(t => t.ForJob(reminders)
            .WithSimpleSchedule(s => s.WithIntervalInSeconds(20).RepeatForever()));
            confige.UseMicrosoftDependencyInjectionJobFactory();
        });
        services.AddCustomHealthChecks(configuration);
        services.AddQuartzHostedService();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ISmsService, SmsService>();
        services.AddScoped<IApplicationUserManager, ApplicationUserManager>();
        services.AddScoped<IApplicationRoleManager, ApplicationRoleManager>();
        services.AddScoped<ITokenProvider, TokenProvider>();
        services.AddScoped<IAdminAccess, AdminAccess>();
        services.AddSingleton<IUserAccessGuard, UserAccessGuard>();
        services.AddScoped<IAdminIdentityService, AdminIdentityService>();

        // موتور ربات در پس‌زمینه؛ اتصال‌ها/جریان‌هایی که در این اسمبلی نوشته شوند هم پیدا می‌شوند.
        services.AddHostedService<BotDispatcherService>();
        services.AddHostedService<BroadcastDispatcherService>();
        services.AddHostedService<AssistantDigestDispatcherService>();
        services.AddBotParts(typeof(ConfigureServices).Assembly);
        services.AddTransient<ICacheService, CacheService>();
        services.AddScoped<ICsvFileBuilder,CsvFileBuilder>();
        services.AddSingleton<IUploaderService, UploadService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddSingleton<IChatFileStorage, ChatFileStorage>();
        return services;
    }

    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        string sqlConnectionString = configuration.GetConnectionString(nameof(ApplicationDbContext))!;

        services.AddHealthChecks()
            .AddSqlServer(sqlConnectionString, name: "sqlserver");
        services.AddScoped<IMessagePurger, MessagePurger>();
        services.AddScoped<IChatFileStreamer, ChatFileStreamer>();
        return services;
    }

}
