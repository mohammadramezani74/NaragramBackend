using CleanArchitecture.Domain.Entities.Chat;
using CleanArchitecture.Domain.Entities.Identity;
using CleanArchitecture.Domain.Entities.ToDoItems;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.unitOfWork
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        public Task<Result> SaveChangesAsync(CancellationToken cancellationToken=default);
    }
    public interface IApplicationUnitOfWork : IUnitOfWork
    {
        public DbSet<User> Users { get; }
        public DbSet<RefreshToken> TokenProvider { get;  }
        public DbSet<TodoItem> TodoItem { get; }
        public DbSet<Conversation> Conversation { get;  }
        public DbSet<Message> Messages { get; }
        public DbSet<ChatFiles> ChatFiles { get; }
        public DbSet<UserAvatar> UserAvatars { get; }
        public DbSet<MessageReaction> MessageReactions { get; }
        public DbSet<FireBaseToken> FireBaseTokens { get; }

    }
}
