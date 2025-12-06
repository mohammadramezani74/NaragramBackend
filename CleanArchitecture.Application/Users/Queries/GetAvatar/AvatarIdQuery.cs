using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Users.Queries.GetAvatar
{
    public sealed record AvatarIdQuery(Guid fileId,bool isfromchannel) : IQuery<(byte[]? Bytes, string Name)>;
}
