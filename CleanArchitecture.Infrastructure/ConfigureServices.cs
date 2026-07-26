using CleanArchitecture.Application.Abstraction;
using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Abstraction.Caching;
using CleanArchitecture.Application.Abstraction.CsvFiles;
using CleanArchitecture.Application.Abstraction.Sms;
using CleanArchitecture.Application.Abstraction.Storage;
using CleanArchitecture.Application.Abstraction.Uploader;
using CleanArchitecture.Infrastructure.Authentication;
using CleanArchitecture.Infrastructure.BackgroundJob;
using CleanArchitecture.Infrastructure.Caching;
using CleanArchitecture.Infrastructure.Files;
using CleanArchitecture.Infrastructure.HealthChecks;
using CleanArchitecture.Infrastructure.Notification;
using CleanArchitecture.Infrastructure.Persistence;
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
            confige.UseMicrosoftDependencyInjectionJobFactory();
        });
        services.AddCustomHealthChecks(configuration);
        services.AddQuartzHostedService();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ISmsService, SmsService>();
        services.AddScoped<IApplicationUserManager, ApplicationUserManager>();
        services.AddScoped<IApplicationRoleManager, ApplicationRoleManager>();
        services.AddScoped<ITokenProvider, TokenProvider>();
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

        return services;
    }

}
