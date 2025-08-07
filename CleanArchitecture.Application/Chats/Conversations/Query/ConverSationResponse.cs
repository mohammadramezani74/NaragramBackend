using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Domain.Entities.Chat;
using CleanArchitecture.Domain.Entities.Identity;
using Mapster;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Conversations.Query
{
    public sealed class ConverSationResponse : IRegister
    {
        public Guid Id { get; set; }
        public string ChatType { get; set; }
        public string Title { get; set; } = null!;
       public List< UserViewModel> Users { get; set; }

        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<CleanArchitecture.Domain.Entities.Chat.Conversation, ConverSationResponse>()
                 .Map(des => des.ChatType, src => src.IsPrivate ? "شخصی" : "گروه")
                 .Map(des => des.Users, src => src.Users.Adapt<List<UserViewModel>>());
            ;
            config.NewConfig<ConversationUser, UserViewModel>()

               .Map(x=>x.Name, s =>string.Concat( s.User.LastName," ", s.User.FirsName))
       .Map(dest => dest.Avatar, src =>
        src.User != null && src.User.UserAvatars.Count>10
            ? Convert.ToBase64String(src.User.UserAvatars.First().FileData)
            : null
    )
                  .Map(x=>x.Id, s => s.User.Id)
                ;
            
        }
        
    }
}
