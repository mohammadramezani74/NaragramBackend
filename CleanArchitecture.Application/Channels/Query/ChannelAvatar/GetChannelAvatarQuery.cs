using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Users.Queries.GetUserAvatar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Query.ChannelAvatar
{
    public sealed record GetChannelAvatarQuery(Guid ChannelId) : IQuery<GetUserAvatarQueryResponse>;

}
