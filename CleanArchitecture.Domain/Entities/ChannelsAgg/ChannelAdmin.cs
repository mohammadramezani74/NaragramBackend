using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Entities.ChannelsAgg
{
    public class ChannelAdmin:BaseEntity
    {
        public Guid ChannelId { get; internal set; }
        public Channel Channel { get; internal set; }

        public Guid UserId { get; internal set; }
        public User User { get; internal set; }
        public bool CanDelete { get; internal set; }
        public bool CanEdit { get; internal set; }
        public bool CanPin { get; internal set; }
        public static ChannelAdmin Create(Guid channelId, Guid userId, bool canDelete, bool canEdit, bool canPin)
            => new ChannelAdmin
            {
                Id = Guid.NewGuid(),
                CreateDate = DateTime.Now,
                Deleted = false,
                CanDelete = canDelete,
                CanEdit = canEdit,
                CanPin = canPin,
                ChannelId = channelId,
                UserId = userId,

            };
    }
}
