using CleanArchitecture.Application.Common.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Hubs.Services
{
    public class UserOnlineService
    {
       
        public ConcurrentDictionary<string, OnlineUsers> OnlineUsers = new ConcurrentDictionary<string, OnlineUsers>();

     
        public void AddOnlineUsers(Guid userId, string connectionId)
        {
            var user = new OnlineUsers(userId, connectionId);
            OnlineUsers.TryAdd(connectionId, user); 
        }

   
        public IReadOnlyCollection<string> GetUserConnectionIds(Guid userId)
        {
            return OnlineUsers.Values
                .Where(user => user.UserId == userId)
                .Select(user => user.ConnectionId)
                .ToList();
        }

   
        public void RemoveByConnectionId(string connectionId)
        {
            OnlineUsers.TryRemove(connectionId, out _); 
        }
    }
}
