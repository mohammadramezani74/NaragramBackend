using CleanArchitecture.Application.Abstraction;
using CleanArchitecture.Application.Bots.Models;
using CleanArchitecture.Application.Chats.Messages;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Hubs;
using CleanArchitecture.Application.Hubs.Abstractions;
using CleanArchitecture.Application.Hubs.Models;
using CleanArchitecture.Domain.Entities.Chat;
using CleanArchitecture.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArchitecture.Application.Bots.Engine
{
    /// <summary>
    /// فرستادن و ویرایش پیام از طرف ربات. پیام ربات یک پیام عادی در گفتگوی خصوصی
    /// است؛ پس لیست چت، شمارش نخوانده، اعلان و تاریخچه مثل پیام آدم‌ها کار می‌کنند.
    /// </summary>
    internal sealed class BotMessenger(
        IApplicationUnitOfWork uow,
        IHubContext<NaraHub, IChatHubClient> hub,
        INotificationService notifications,
        ILogger<BotMessenger> logger)
    {
        private const int MaxContentLength = 4096;

        public async Task<Message> SendAsync(BotTurn turn, string text, BotReplyMarkup? markup = null)
        {
            text = Clip(text);
            var message = turn.Conversation.AddMessage(text, turn.BotUser);
            message.SetReplyMarkup(markup?.ToJson());
            await AfterAddAsync(turn, message, Preview(text), null);
            return message;
        }

        public async Task<Message> SendDocumentAsync(BotTurn turn, byte[] data, string fileName, string? caption)
        {
            var file = ChatFiles.CreateDocument(data, Path.GetExtension(fileName), fileName, data.LongLength, turn.BotUser.Id);
            var message = turn.Conversation.AddMessage(
                Clip(caption ?? string.Empty), turn.BotUser, null, new List<ChatFiles> { file }, MessageType.Document);

            var fileDto = new ChatFilesDto { FileId = file.Id, FileName = file.FileName, FileSize = file.FileSize.ToString() };
            await AfterAddAsync(turn, message, string.IsNullOrWhiteSpace(caption) ? "فایل" : Preview(caption), fileDto);
            return message;
        }

        /// <param name="newText">null یعنی متن پیام عوض نشود.</param>
        /// <param name="markEdited">false برای جابه‌جایی در منو، تا برچسب «ویرایش‌شده» نخورد.</param>
        public async Task EditAsync(BotTurn turn, Guid messageId, string? newText, BotReplyMarkup? markup, bool markEdited)
        {
            var ct = turn.CancellationToken;
            var message = await uow.Messages.FirstOrDefaultAsync(m => m.Id == messageId
                                                                      && m.ConversationId == turn.Conversation.Id
                                                                      && m.CreatedByUserId == turn.BotUser.Id
                                                                      && !m.Deleted, ct);
            if (message is null)
                return;

            var content = newText is null ? message.Content : Clip(newText);
            message.EditByBot(content, markup?.ToJson(), markEdited);

            if (newText is not null && turn.Conversation.LastMessageId == message.Id)
                turn.Conversation.LastMessageText = Preview(content);

            await uow.SaveOrThrowAsync(ct);

            try
            {
                await hub.Clients.User(turn.Human.Id.ToString()).BotMessageUpdated(ToDto(turn, message, null));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Bot message update push failed for {MessageId}", messageId);
            }
        }

        /// <summary>پیام کاربر به ربات «دیده شد» (دو تیک).</summary>
        public async Task MarkSeenAsync(BotTurn turn, Guid userMessageId)
        {
            var updated = await uow.Messages
                .Where(m => m.Id == userMessageId
                            && m.ConversationId == turn.Conversation.Id
                            && m.CreatedByUserId == turn.Human.Id
                            && !m.Seen)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.Seen, true), turn.CancellationToken);

            if (updated == 0)
                return;

            try
            {
                await hub.Clients.User(turn.Human.Id.ToString()).MessagedSeenReceived(new List<Guid> { userMessageId });
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Bot seen push failed");
            }
        }

        public async Task PushStateAsync(BotTurn turn, BotStateDto state)
        {
            try
            {
                await hub.Clients.User(turn.Human.Id.ToString()).BotStateChanged(state);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Bot state push failed");
            }
        }

        private async Task AfterAddAsync(BotTurn turn, Message message, string preview, ChatFilesDto? file)
        {
            var ct = turn.CancellationToken;
            var conversation = turn.Conversation;
            conversation.LastMessageText = preview;
            conversation.LastMessageId = message.Id;
            conversation.LastUserSenderMessageId = turn.BotUser.Id;
            conversation.LastMessageSentAt = message.CreateDate;
            turn.HumanMembership.IncreaseCount();

            await uow.SaveOrThrowAsync(ct);

            var userKey = turn.Human.Id.ToString();
            try
            {
                await hub.Clients.User(userKey).IncreaseMessageCount(turn.BotUser.Id);
                await hub.Clients.User(userKey).MessagedReceived(ToDto(turn, message, file));
                await hub.Clients.User(userKey).ReceivedNotifications(
                    new NotificationModelDto(turn.BotName, turn.BotUser.Avatar ?? string.Empty, preview, string.Empty));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Bot message push failed for {MessageId}", message.Id);
            }

            // اعلان صفحه‌ی بسته. توکن‌ها همین‌جا خوانده می‌شوند تا ارسال در پس‌زمینه
            // به DbContext این درخواست دست نزند؛ SendToMany هیچ‌وقت استثنا نمی‌دهد.
            var tokens = await uow.FireBaseTokens.AsNoTracking()
                .Where(t => t.UserId == turn.Human.Id)
                .Select(t => t.Token)
                .ToListAsync(ct);

            if (tokens.Count > 0)
                _ = notifications.SendToMany(tokens, turn.BotName, preview);
        }

        private static ChatMessageDto ToDto(BotTurn turn, Message message, ChatFilesDto? file) => new()
        {
            Id = message.Id,
            ScopeId = turn.Conversation.Id,
            Content = message.Content,
            IsMine = false,
            SendAt = message.CreateDate,
            SenderName = turn.BotName,
            Type = (int)message.MessageType,
            UserId = turn.BotUser.Id,
            IsSeen = false,
            ParentId = message.ParentMessageId,
            FileContent = file,
            ReplyMarkup = message.ReplyMarkup,
            ConversationType = ConversationTyped.Private,
        };

        private static string Clip(string text)
        {
            text ??= string.Empty;
            return text.Length > MaxContentLength ? text[..MaxContentLength] : text;
        }

        private static string Preview(string text)
        {
            var firstLine = (text ?? string.Empty).Split('\n')[0].Trim();
            return firstLine.Length > 30 ? firstLine[..30] + "..." : firstLine;
        }
    }
}
