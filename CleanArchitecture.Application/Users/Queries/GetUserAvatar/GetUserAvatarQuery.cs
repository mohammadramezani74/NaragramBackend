using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Queries.GetUserAvatar
{
    public sealed record GetUserAvatarQuery(Guid UserId):IQuery<GetUserAvatarQueryResponse>;
 
}
