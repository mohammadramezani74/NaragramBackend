using CleanArchitecture.Application.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Groups.Query.GroupMembers
{
    public sealed record GetGroupMembersQuery(Guid ConversationId):IQuery<IReadOnlyList<GroupMemberViewModel>>;
}
