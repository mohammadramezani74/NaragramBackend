using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Infrastructure.Persistence.Interceptors;
using CleanArchitecture.Infrastructure.Persistence.UnitofWork;
using EFCoreSecondLevelCacheInterceptor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Text;

namespace CleanArchitecture.Infrastructure.Persistence;

public static class ConfigureServices
{
    public static IServiceCollection RegisterPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        

        services.AddEFSecondLevelCache(options =>
        {
            options.UseMemoryCacheProvider() 
                   .CacheAllQueries(CacheExpirationMode.Sliding, TimeSpan.FromMinutes(15)) 
                   .ConfigureLogging(true); 
        });
        services.AddScoped< DispatchDomainEventsInterceptor>();
        services.AddScoped<CustomSecondLevelCacheInterceptor>();
        services.AddScoped<AuditInterceptor>();
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var Audiinterceptor = serviceProvider.GetRequiredService<AuditInterceptor>();
            var Outboxinterceptor = serviceProvider.GetRequiredService<DispatchDomainEventsInterceptor>();
            var secondLevelCache = serviceProvider.GetRequiredService<CustomSecondLevelCacheInterceptor>();
            var appSettings = serviceProvider.GetRequiredService<IOptions<ApplicationSettings>>().Value;
           if( appSettings is null)  throw new ArgumentNullException(nameof(appSettings));
      
            options
            .UseSqlServer(appSettings.ConnectionStrings.ApplicationDbContext)
            .AddInterceptors(Audiinterceptor, secondLevelCache, Outboxinterceptor); });
        services.AddScoped<IApplicationUnitOfWork, ApplicationUnitOfWork>();




        return services;
    }

}

