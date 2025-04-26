using CleanArchitecture.Infrastructure.Persistence;
using CleanArchitecture.Infrastructure.Persistence.Outbox;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Quartz;
using SharedKernel;

namespace CleanArchitecture.Infrastructure.BackgroundJob
{
    internal class ProcessOutBoxMessagesJob : IJob
    {
        private readonly ApplicationDbContext _context;
        private readonly IPublisher _publisher;
        private readonly IDateTimeProvider _dateTimeProvider;

        public ProcessOutBoxMessagesJob(ApplicationDbContext context, IPublisher publisher, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _publisher = publisher;
            _dateTimeProvider = dateTimeProvider;
        }


        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
            var messages=await _context.Set<OutboxMessage>()
                .Where(m=>m.Processed==null)
                .Take(50)
                .ToListAsync(context.CancellationToken);
            foreach (var outboxMessage in messages)
            {
                IDomainEvent? domainevent=JsonConvert.DeserializeObject<IDomainEvent>(outboxMessage.Content,new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
                if (domainevent is null)
                    continue;
                await _publisher.Publish(domainevent,context.CancellationToken);
                outboxMessage.Processed= _dateTimeProvider.Now;

            }
            if(messages.Any()) { 
            await _context.SaveChangesAsync(context.CancellationToken);
                }
            }
            catch (Exception)
            {

            
            }
        }
    }
}
