using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Entities.ChannelsAgg
{
    public class ChannelMember : BaseEntity
    {
        public Guid ChannelId { get; internal set; }
        public Channel Channel { get; internal set; }

        public Guid UserId { get; internal set; }
        public User User { get; internal set; }
        public int UnreadCount { get; internal set; }
        public void IncreaseCount() => UnreadCount++;
        public void DecreaseCount() => UnreadCount--;
        public void EmptyCount() => UnreadCount = 0;

        public static ChannelMember Join(Guid channelId, Guid userId)
        {
            return new ChannelMember
            {
                Id = Guid.NewGuid(),
                ChannelId = channelId,
                UserId = userId,
                CreateDate = DateTime.Now,
                Deleted = false,
                UnreadCount = 0
            };
        }
        public static List<ChannelMember> JoinAllMembers(Guid channelId,IEnumerable<Guid >usersId)
        { var ListAllMembers=new List<ChannelMember>();
            foreach (var user in usersId)
            {
                ListAllMembers.Add(new ChannelMember
                {
                    Id = Guid.NewGuid(),
                    ChannelId = channelId,
                    UserId = user,
                    CreateDate = DateTime.Now,
                    Deleted = false,
                    UnreadCount = 0
                });
            }
            return ListAllMembers;
        }


        public void Leave()
        {
            this.ModifiedDate = DateTime.Now;
        }
    }
}
