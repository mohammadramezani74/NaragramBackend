using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Query.ChannelMembers
{
    public sealed record class ChannelMemberQuery(Guid ChannelId):IQuery<IReadOnlyList<ChannelMemberViewModel>>;

}
