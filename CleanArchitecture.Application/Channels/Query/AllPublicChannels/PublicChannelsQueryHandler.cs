using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Users.Queries.GetUser;
using CleanArchitecture.Domain.Entities.ChannelsAgg;
using CleanArchitecture.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Channels.Query.AllPublicChannels
{
    internal sealed class PublicChannelsQueryHandler(IApplicationUnitOfWork uow) : IQueryHandler<PublicChannelsQuery, IReadOnlyList<GetUserResponse>>
    {
        private readonly IApplicationUnitOfWork _uow = uow;

        public async Task<OperationResult<IReadOnlyList<GetUserResponse>>> Handle(PublicChannelsQuery request, CancellationToken cancellationToken)
        {
            var channelsquery = await _uow.Channels
                .Include(x => x.Members)
            .Include(x => x.CreatedByUser)
                .Include(x => x.Admins).ThenInclude(x => x.User)
                .Where(x=>x.IsPublic)
          .ToListAsync(cancellationToken);
            var channels = channelsquery
        .Select(x => new GetUserResponse
        {
            Id = x.Id,
            FirstName = x.Title,
            UserName = x.UserName,
            LastReceivedMessage = x.LastMessageText,
            LastReceivedMessageId = x.LastMessageId,
            IsLastReceivedMessageForMe = false,
            LastReceivedMessageSendDate = CreateDateTime(x.LastMessageSentAt),
            IsChannel = true,
            channel = new ChannelDto
            {
                Creator = x.CreatedByUser.LastName + " " + x.CreatedByUser.FirsName,
                CreatorId = x.CreatedByUserId.Value,
                admins = GetAdmins(x.Admins, x.CreatedByUserId),
                CurrentUserAdmin = false
            }
        }).ToList();
            return channels;

        }

        private static string CreateDateTime(DateTime? lastMessageSentAt)
        {
            return lastMessageSentAt?.FormatPersianDate().ToPersianNumber() ?? "";
        }
        private static List<UserChannelDto> GetAdmins(IReadOnlyCollection<ChannelAdmin> admins, Guid? CreatorId)
        {
            var list = new List<UserChannelDto>();
            foreach (var admin in admins.Where(x => x.UserId != CreatorId).ToList())
            {
                list.Add(new UserChannelDto
                {
                    Id = admin.UserId,
                    IsAdmin = true,
                    Name = admin.User.FirsName + " " + admin.User.LastName
                });
            }
            return list;
        }
        private static bool IsUserAdmin(Channel x, Guid userId)
        {
            if (x.CreatedByUserId == userId)
                return true;
            else if (x.Admins.Any(x => x.UserId == userId))
            {
                return true;
            }
            return false;
        }
    }
}
