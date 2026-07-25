using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Groups.Query.GetAvatar
{
    public sealed record  GroupAvatarQuery(Guid FileId):IQuery<(byte[]? Bytes, string Name)>;

}
