using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Users.Queries.GetUser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Query.AllPublicChannels
{
    public sealed record PublicChannelsQuery():IQuery<IReadOnlyList<GetUserResponse>>;
}
