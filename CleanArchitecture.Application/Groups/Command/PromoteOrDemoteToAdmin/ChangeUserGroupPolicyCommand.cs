using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Domain.Entities.ChannelsAgg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Groups.Command.PromoteOrDemoteToAdmin
{
    public sealed record ChangeUserGroupPolicyCommand(Guid conversationId, Guid UserId, bool ispromote) : ICommands;
}
