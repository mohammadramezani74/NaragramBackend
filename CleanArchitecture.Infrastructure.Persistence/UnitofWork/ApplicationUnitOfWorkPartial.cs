using CleanArchitecture.Domain.Entities.ChannelsAgg;
using CleanArchitecture.Domain.Entities.Chat;
using CleanArchitecture.Domain.Entities.Identity;
using CleanArchitecture.Domain.Entities.ToDoItems;
using CleanArchitecture.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Persistence.UnitofWork
{
    public partial class ApplicationUnitOfWork
    {
        public DbSet<User> Users => _context.Set<User>();
        public DbSet<RefreshToken> TokenProvider => _context.Set<RefreshToken>();
        public DbSet<TodoItem> TodoItem => _context.Set<TodoItem>();
        public DbSet<OutboxMessage> OutboxMessage => _context.Set<OutboxMessage>();
        public DbSet<Conversation> Conversation => _context.Set<Conversation>();
        public DbSet<ConversationUser> ConversationUser => _context.Set<ConversationUser>();
        public DbSet<Message> Messages => _context.Set<Message>();
        public DbSet<ChatFiles> ChatFiles => _context.Set<ChatFiles>();
        public DbSet<UserAvatar> UserAvatars => _context.Set<UserAvatar>();
        public DbSet<MessageReaction> MessageReactions => _context.Set<MessageReaction>();
        public DbSet<FireBaseToken> FireBaseTokens =>_context.Set<FireBaseToken>();
        public DbSet<Channel> Channels => _context.Set<Channel>();
        public DbSet<ChannelMember> ChannelMembers => _context.Set<ChannelMember>();
        public DbSet<ChannelAdmin> ChannelAdmins => _context.Set<ChannelAdmin>();
        public DbSet<ChannelMessageSeen> ChannelMessageSeens => _context.Set<ChannelMessageSeen>();
        public DbSet<ChannelInvite> ChannelInvites => _context.Set<ChannelInvite>();
        public DbSet<ChannelAvatar> ChannelAvatars => _context.Set<ChannelAvatar>();
        public DbSet<ConversationAvatar> ConversationAvatar => _context.Set<ConversationAvatar>();
    }
}
