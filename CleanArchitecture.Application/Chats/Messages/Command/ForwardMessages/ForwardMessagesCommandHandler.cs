using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Hubs.Models;
using CleanArchitecture.Domain.Entities.ChannelsAgg;
using CleanArchitecture.Domain.Entities.Chat;
using CleanArchitecture.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Chats.Messages.Command.ForwardMessages
{
    internal sealed class ForwardMessagesCommandHandler(
        IApplicationUnitOfWork uow,
        IApplicationUserManager userManager,
        IHubContext<NaraHub, IChatHubClient> hubContext)
        : ICommandHandler<ForwardMessagesCommand, int>
    {
        private const int MaxMessages = 20;
        private const int MaxTargets = 10;

        private readonly IApplicationUnitOfWork _uow = uow;
        private readonly IApplicationUserManager _userManager = userManager;
        private readonly IHubContext<NaraHub, IChatHubClient> _hubContext = hubContext;

        public async Task<OperationResult<int>> Handle(
            ForwardMessagesCommand request, CancellationToken ct)
        {
            var op = new OperationResult();

            if (request.MessageIds is null || request.MessageIds.Count == 0)
                return OperationResult.Failure<int>(op.Failed("پیامی برای فوروارد انتخاب نشده است!"));

            if (request.Targets is null || request.Targets.Count == 0)
                return OperationResult.Failure<int>(op.Failed("مقصدی برای فوروارد انتخاب نشده است!"));

            if (request.MessageIds.Count > MaxMessages)
                return OperationResult.Failure<int>(op.Failed($"حداکثر {MaxMessages} پیام در هر بار قابل فوروارد است!"));

            if (request.Targets.Count > MaxTargets)
                return OperationResult.Failure<int>(op.Failed($"حداکثر {MaxTargets} مقصد در هر بار قابل انتخاب است!"));

            var myId = _userManager.UserId!.Value;
            var me = await _userManager.GetUserBy(myId);

            var sourceIds = request.MessageIds.Distinct().ToList();

            // پیام‌های مبدأ، فقط آن‌هایی که واقعاً به آن‌ها دسترسی دارم.
            // ChatFiles با Include می‌آید چون شناسه‌ی فایل‌ها برای ساختن ردیف
            // واسط لازم است — خودِ بایت‌ها لود نمی‌شوند مگر پروجکشن بخواهد.
            var sources = await _uow.Messages
                .AsNoTracking()
                .Include(m => m.ChatFiles)
                .Where(m => sourceIds.Contains(m.Id)
                         && (m.Conversation!.Users.Any(u => u.UserId == myId)
                          || m.Channel!.Members.Any(mm => mm.UserId == myId)))
                .ToListAsync(ct);

            if (sources.Count == 0)
                return OperationResult.Failure<int>(op.Failed("پیام‌های انتخاب‌شده یافت نشدند!"));

            // ترتیب اصلی حفظ می‌شود، نه ترتیبی که دیتابیس برگردانده.
            sources = sources
                .OrderBy(m => m.CreateDate)
                .ToList();

            var senderName = string.Concat(me.LastName, " ", me.FirsName).Trim();
            var created = 0;

            foreach (var target in request.Targets)
            {
                var resolved = await ResolveTargetAsync(target, myId, ct);

                if (!resolved.Ok)
                    return OperationResult.Failure<int>(op.Failed(resolved.Error!));

                var dtos = new List<ChatMessageDto>();

                // پایه‌ی زمان برای این مقصد. هر پیام یک میلی‌ثانیه جلوتر می‌رود
                // تا صفحه‌بندی که بر اساس CreateDate است ترتیب اصلی را حفظ کند؛
                // با زمان یکسان ترتیبشان در بارگذاری بعدی قطعی نیست.
                var baseTime = DateTime.Now;

                for (var i = 0; i < sources.Count; i++)
                {
                    var src = sources[i];
                    var when = baseTime.AddMilliseconds(i);

                    var copy = resolved.Channel is not null
                        ? Message.AddForChannelMessage(
                            src.Content, resolved.Channel.Id, myId, null, null, src.MessageType)
                        : resolved.Conversation!.AddMessage(
                            src.Content, me!, null, null, src.MessageType);

                    copy.CreateDate = when;

                    // زنجیره‌ی فوروارد همیشه به نفر اول اشاره می‌کند، نه به واسطه
                    copy.MarkAsForwarded(src.ForwardedFromName, ResolveOriginalName(src, senderName));

                    if (resolved.Channel is not null)
                        _uow.Messages.Add(copy);

                    // فایل‌ها بدون کپی شدن بایت‌ها به پیام تازه وصل می‌شوند.
                    // ردیف واسط صریح ساخته می‌شود و نه از طریق copy.ChatFiles،
                    // چون سازنده‌های پیام آن مجموعه را وقتی فایلی نباشد null
                    // می‌گذارند.
                    foreach (var file in src.ChatFiles)
                        _uow.MessageFiles.Add(MessageFile.Link(copy.Id, file.Id));

                    dtos.Add(new ChatMessageDto
                    {
                        Id = copy.Id,
                        ScopeId = resolved.ScopeId,
                        UserId = resolved.Channel is not null ? resolved.Channel.Id : myId,
                        SenderName = senderName,
                        Content = copy.Content ?? string.Empty,
                        IsMine = false,
                        IsSeen = false,
                        SendAt = copy.CreateDate,
                        Type = (int)copy.MessageType,
                        ConversationType = resolved.Type,
                        FileContent = src.ChatFiles.Select(f => new ChatFilesDto
                        {
                            FileId = f.Id,
                            FileName = f.FileName,
                            FileSize = f.FileSize.ToString()
                        }).FirstOrDefault()
                    });

                    created++;
                }

                UpdateLastMessage(resolved, dtos[^1], myId);

                await _uow.SaveChangesAsync(ct);

                await BroadcastAsync(resolved, dtos, myId, ct);
            }

            return created;
        }

        /// <summary>
        /// برچسب مبدأ برای پیامی که خودش فوروارد نبوده: نام فرستنده‌ی همان پیام.
        /// اگر کاربر پیام خودش را فوروارد کند، نام خودش می‌نشیند که درست است.
        /// </summary>
        private static string ResolveOriginalName(Message src, string fallback)
        {
            var user = src.CreatedByUser;

            return user is null
                ? fallback
                : string.Concat(user.LastName, " ", user.FirsName).Trim();
        }

        private sealed class ResolvedTarget
        {
            public bool Ok { get; init; }
            public string? Error { get; init; }
            public Conversation? Conversation { get; init; }
            public Channel? Channel { get; init; }
            public Guid ScopeId { get; init; }
            public ConversationTyped Type { get; init; }
        }

        private async Task<ResolvedTarget> ResolveTargetAsync(
            ForwardTarget target, Guid myId, CancellationToken ct)
        {
            if (target.Type == ConversationTyped.Channel)
            {
                var channel = await _uow.Channels
                    .Include(c => c.Members)
                    .FirstOrDefaultAsync(c => c.Id == target.Id, ct);

                if (channel is null)
                    return new ResolvedTarget { Error = "کانال مقصد یافت نشد!" };

                if (!channel.Members.Any(m => m.UserId == myId))
                    return new ResolvedTarget { Error = "شما عضو این کانال نیستید!" };

                return new ResolvedTarget
                {
                    Ok = true,
                    Channel = channel,
                    ScopeId = channel.Id,
                    Type = ConversationTyped.Channel
                };
            }

            if (target.Type == ConversationTyped.group)
            {
                var conversation = await _uow.Conversation
                    .Include(c => c.Users)
                    .FirstOrDefaultAsync(c => c.Id == target.Id, ct);

                if (conversation is null)
                    return new ResolvedTarget { Error = "گروه مقصد یافت نشد!" };

                var membership = conversation.Users.FirstOrDefault(u => u.UserId == myId);

                if (membership is null)
                    return new ResolvedTarget { Error = "شما عضو این گروه نیستید!" };

                if (membership.IsMuted)
                    return new ResolvedTarget { Error = "یوزر شما در این گروه سکوت خورده است!" };

                return new ResolvedTarget
                {
                    Ok = true,
                    Conversation = conversation,
                    ScopeId = conversation.Id,
                    Type = ConversationTyped.group
                };
            }

            // خصوصی: Id شناسه‌ی کاربر مقابل است.
            if (target.Id == myId)
                return new ResolvedTarget { Error = "امکان فوروارد به خودتان وجود ندارد!" };

            var other = await _userManager.GetUserBy(target.Id);

            if (other is null)
                return new ResolvedTarget { Error = "کاربر مقصد یافت نشد!" };

            var pairIds = new List<Guid> { myId, other.Id };

            var existing = await _uow.Conversation
                .Include(c => c.Users)
                .FirstOrDefaultAsync(c => c.IsPrivate
                                       && c.Users.Count == 2
                                       && c.Users.All(u => pairIds.Contains(u.UserId)), ct);

            if (existing is null)
            {
                // طبق توافق، نبودِ گفتگو خطا نیست: ساخته می‌شود تا کاربر بتواند
                // به هر کسی در لیست مخاطبانش فوروارد کند.
                var me = await _userManager.GetUserBy(myId);
                existing = Conversation.Create(me!, other);
                _uow.Conversation.Add(existing);
            }

            return new ResolvedTarget
            {
                Ok = true,
                Conversation = existing,
                ScopeId = existing.Id,
                Type = ConversationTyped.Private
            };
        }

        private static void UpdateLastMessage(ResolvedTarget target, ChatMessageDto last, Guid myId)
        {
            var preview = Preview(last);

            if (target.Channel is not null)
            {
                target.Channel.LastMessageText = preview;
                target.Channel.LastMessageId = last.Id;
                target.Channel.LastMessageSentAt = last.SendAt;
                return;
            }

            target.Conversation!.LastMessageText = preview;
            target.Conversation.LastMessageId = last.Id;
            target.Conversation.LastUserSenderMessageId = myId;
            target.Conversation.LastMessageSentAt = last.SendAt;
        }

        private static string Preview(ChatMessageDto dto)
        {
            var text = (MessageType)dto.Type switch
            {
                MessageType.Video => "پیام ویدیویی",
                MessageType.Audio => "پیام صوتی",
                MessageType.Image => "پیام تصویری",
                MessageType.Document => "پیام  اسنادی",
                MessageType.Location => "لوکیشن",
                _ => dto.Content ?? string.Empty
            };

            return text.Length > 15 ? text.Substring(0, 15) + "..." : text;
        }

        private async Task BroadcastAsync(
            ResolvedTarget target, List<ChatMessageDto> dtos, Guid myId, CancellationToken ct)
        {
            var excluded = NaraHub.GetUserConnections(myId);

            foreach (var dto in dtos)
            {
                if (target.Channel is not null)
                {
                    await _hubContext.Clients
                        .GroupExcept(target.Channel.Id.ToString(), excluded)
                        .MessagedReceived(dto);
                }
                else if (target.Type == ConversationTyped.group)
                {
                    await _hubContext.Clients
                        .GroupExcept(target.Conversation!.Id.ToString(), excluded)
                        .MessagedReceived(dto);
                }
                else
                {
                    var otherId = target.Conversation!.Users
                        .Select(u => u.UserId)
                        .FirstOrDefault(id => id != myId);

                    if (otherId != Guid.Empty)
                        await _hubContext.Clients.User(otherId.ToString()).MessagedReceived(dto);
                }
            }
        }
    }
}
