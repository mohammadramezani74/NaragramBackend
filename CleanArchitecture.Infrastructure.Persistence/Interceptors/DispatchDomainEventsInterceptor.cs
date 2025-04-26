using CleanArchitecture.Domain.Common;
using CleanArchitecture.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;

namespace CleanArchitecture.Infrastructure.Persistence.Interceptors
{
    public class DispatchDomainEventsInterceptor : SaveChangesInterceptor
    {




        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;
            if (context == null) {
                return await base.SavingChangesAsync(eventData, result, cancellationToken);
            }

            var entities = context.ChangeTracker
                .Entries<BaseEntity>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity);

            var domainEvents = entities
                .SelectMany(e => e.DomainEvents)
                .Select(domainevent => new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Occured = DateTime.Now,
                    Type = domainevent.GetType().Name,
                    Content = JsonConvert.SerializeObject(domainevent, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All })

                })
                .ToList();


            entities.ToList().ForEach(e => e.ClearDomainEvents());
            context.Set<OutboxMessage>().AddRange(domainEvents);

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

    
    }
}
