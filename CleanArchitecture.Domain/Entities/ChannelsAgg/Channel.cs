using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Entities.Identity;
using CleanArchitecture.Domain.Exceptions;
using Microsoft.Extensions.Internal;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Entities.ChannelsAgg
{
    public sealed class Channel : BaseEntity
    {
        public string Title { get; private set; } = null!;
        public string Description { get;private set; } = string.Empty;
        public string UserName { get;private set; } = string.Empty;
        public bool IsPublic { get;private set; } = false;
        private readonly List<ChannelAdmin> _admins = new();
        private readonly List<ChannelMember> _members = new();
        private readonly List<ChannelInvite> _invites = new();

        public IReadOnlyCollection<ChannelAdmin> Admins => _admins.AsReadOnly();
        public IReadOnlyCollection<ChannelMember> Members => _members.AsReadOnly();
        public IReadOnlyCollection<ChannelInvite> Invites => _invites.AsReadOnly();
        public Guid? LastMessageId { get; set; }
        public string? LastMessageText { get; set; }
        public Guid? LastUserSenderMessageId { get; set; }
        public DateTime? LastMessageSentAt { get; set; }

        public static Channel CreatePublicChannel(string title,
    string userName,
    string description,
    Guid creatorUserId)
        {


            var channel = new Channel
            { Id = Guid.NewGuid(),
            Deleted = false,
            CreateDate=DateTime.Now,
            CreatedByUserId=creatorUserId,
                Title = title,
                UserName = userName,
                Description = description,
                IsPublic = true
            };
            var member = new ChannelMember
            {Id = Guid.NewGuid(),
            CreateDate=DateTime.Now,
            Deleted = false,
                UserId = creatorUserId,
                Channel = channel
            };
            var admin = new ChannelAdmin
            {Id= Guid.NewGuid(),
            Deleted=false,
            CreateDate = DateTime.Now,
                UserId = creatorUserId,
                Channel = channel,
                CanDelete = true,
                CanEdit = true,
                CanPin = true
            };
            channel._members.Add(member);
            channel._admins.Add(admin);
            return channel;
        }

        public static Channel CreatePrivateChannel(
    string title,
    string userName,
    string description,
    Guid creatorUserId)
        {


            var channel = new Channel
            {
                Id = Guid.NewGuid(),
                Deleted = false,
                CreateDate = DateTime.Now,
                CreatedByUserId = creatorUserId,
                Title = title,
                UserName = userName,
                Description = description,
                IsPublic = false
            };
            var member = new ChannelMember
            {
                Id = Guid.NewGuid(),
                CreateDate = DateTime.Now,
                Deleted = false,
                UserId = creatorUserId,
                Channel = channel
            };
            var admin = new ChannelAdmin
            {
                Id = Guid.NewGuid(),
                Deleted = false,
                CreateDate = DateTime.Now,
                UserId = creatorUserId,
                Channel = channel,
                CanDelete = true,
                CanEdit = true,
                CanPin = true
            };
            channel._members.Add(member);
            channel._admins.Add(admin);
            return channel;
        }
        public void Rename(string title, Guid byUserId)
        {
            EnsureAdmin(byUserId);
            GuardTitle(title);
            Title = title;
        }

        public void ChangeDescription(string description, Guid byUserId)
        {
            EnsureAdmin(byUserId);
            Description = description ?? string.Empty;
        }

        public void Invite(Guid targetUserId, Guid byUserId, DateTime ExpireAt,int MaxUsage)
        {
            EnsureAdmin(byUserId);
            if (IsPublic) throw new DomainException("دعوت فقط در کانال خصوصی معنا دارد.");
            if (IsMember(targetUserId)) return; // idempotent
            if (_invites.Any(i => i.CreatedByUserId == targetUserId && i.ExpireAt<=ExpireAt)) return;
            _invites.Add(ChannelInvite.Create(Id, targetUserId, ExpireAt, MaxUsage));
        }

        public void AcceptInvite(Guid userId, Guid inviteId, DateTime Expiration)
        {
            var inv = _invites.SingleOrDefault(i => i.Id == inviteId && i.CreatedByUserId == userId)
                      ?? throw new DomainException("دعوت معتبر نیست.");
            if (inv.IsExpired(Expiration)) throw new DomainException("دعوت منقضی شده.");
            if (!IsMember(userId))
                _members.Add(ChannelMember.Join(Id, userId));
            inv.MarkUsed(Expiration);
        }
        public void Join(Guid userId, IDateTimeProvider clock)
        {
            if (!IsPublic) throw new DomainException("عضویت مستقیم فقط برای کانال عمومی.");
            if (!IsMember(userId))
                _members.Add(ChannelMember.Join(Id, userId));
        }

        public void Leave(Guid userId)
        {
            var member = _members.SingleOrDefault(m => m.UserId == userId)
                         ?? throw new DomainException("عضو نیستید.");
            if (IsOnlyAdmin(userId)) throw new DomainException("آخرین ادمین نمی‌تواند خارج شود.");
            _members.Remove(member);
            var admin = _admins.SingleOrDefault(a => a.UserId == userId);
            if (admin != null) _admins.Remove(admin);
        }
        public void PromoteToAdmin(Guid userId, Guid byUserId, bool CanDelete, bool CanEdit, bool CanPin)
        {
            EnsureAdmin(byUserId);
          //  if (!IsMember(userId)) throw new DomainException("کاربر عضو کانال نیست.");
            if (_admins.Any(a => a.UserId == userId)) return;
            _admins.Add(ChannelAdmin.Create(Id, userId,CanDelete, CanEdit, CanPin));
        }

        public void DemoteAdmin(Guid userId, Guid byUserId)
        {
            EnsureAdmin(byUserId);
            var admin = _admins.SingleOrDefault(a => a.UserId == userId)
                        ?? throw new DomainException("ادمین یافت نشد.");
            if (IsOnlyAdmin(userId)) throw new DomainException("باید حداقل یک ادمین باقی بماند.");
            _admins.Remove(admin);
        }
        public void RecordLastMessage(Guid messageId, Guid senderUserId, string? text, DateTime sentAtUtc)
        {
            LastMessageId = messageId;
            LastUserSenderMessageId = senderUserId;
            LastMessageText = text;
            LastMessageSentAt = sentAtUtc;
        }
        private static void GuardTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new DomainException("عنوان الزامی است.");
            if (title.Length > 100) throw new DomainException("عنوان خیلی بلند است.");
        }

        private void EnsureAdmin(Guid userId)
        {
            if (!_admins.Any(a => a.UserId == userId))
                throw new DomainException("دسترسی ادمین لازم است.");
        }
        private static void GuardUserName(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) throw new DomainException("نام کاربری الزامی است.");
            if (userName.Length is < 3 or > 32) throw new DomainException("طول نام کاربری نامعتبر.");
            if (!userName.All(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-'))
                throw new DomainException("کاراکتر نامعتبر در نام کاربری.");
        }
        private bool IsMember(Guid userId) => _members.Any(m => m.UserId == userId);
        private bool IsOnlyAdmin(Guid userId) => _admins.Count == 1 && _admins[0].UserId == userId;
    }
}
