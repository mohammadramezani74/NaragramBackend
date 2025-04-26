using EFCoreSecondLevelCacheInterceptor;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Infrastructure.Persistence.Interceptors;

internal sealed class CustomSecondLevelCacheInterceptor : SaveChangesInterceptor
{
    private readonly IEFCacheServiceProvider _cacheServiceProvider;
    private ILogger<CustomSecondLevelCacheInterceptor> _Logger;

    public CustomSecondLevelCacheInterceptor(IEFCacheServiceProvider cacheServiceProvider,
ILogger<CustomSecondLevelCacheInterceptor> logger)    {
        _cacheServiceProvider = cacheServiceProvider;
        _Logger = logger;
    }


    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var changedEntityNames = GetChangedEntityNames(context);

        context.ChangeTracker.AutoDetectChangesEnabled = false;
        var saveResult = await base.SavingChangesAsync(eventData, result, cancellationToken);
        context.ChangeTracker.AutoDetectChangesEnabled = true;

        if (changedEntityNames.Any())
        {
            var cacheKey = new EFCacheKey(new HashSet<string>(changedEntityNames));
            _cacheServiceProvider.InvalidateCacheDependencies(cacheKey);
            _Logger.LogWarning("Invalidated cache for entities: {Entities}", changedEntityNames);
        }

        return saveResult;
    }


    private List<string> GetChangedEntityNames(DbContext context)
    {
        return context.ChangeTracker.Entries()
                      .Where(entry => entry.State == EntityState.Modified || entry.State == EntityState.Added || entry.State == EntityState.Deleted)
                      .Select(entry => entry.Entity.GetType().FullName)
                      .Distinct()
                      .ToList();
    }

}
