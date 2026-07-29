using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Common.Utilities.Text;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Chats.Messages.Query.SearchMessages
{
    internal sealed class SearchMessagesQueryHandler(
            IApplicationUnitOfWork uow,
            IApplicationUserManager userManager)
            : IQueryHandler<SearchMessagesQuery, SearchMessagesResponse>
        {
            private readonly IApplicationUnitOfWork _uow = uow;
            private readonly IApplicationUserManager _userManager = userManager;

            private const int MinTermLength = 2;
            private const int MaxTake = 50;

            public async Task<OperationResult<SearchMessagesResponse>> Handle(
                SearchMessagesQuery request, CancellationToken cancellationToken)
            {
                var op = new OperationResult();
                var myId = _userManager.UserId!.Value;

                var term = PersianText.Normalize(request.Term);
                if (term.Length < MinTermLength)
                    return new SearchMessagesResponse([], null, false);

                if (!await IsMemberAsync(request, myId, cancellationToken))
                    return OperationResult.Failure<SearchMessagesResponse>(
                        op.Failed("دسترسی به این گفتگو ندارید."));

                var pattern = $"%{PersianText.EscapeLike(term)}%";
                var take = Math.Clamp(request.Take, 1, MaxTake);

                var query = _uow.Messages.AsNoTracking()
                    .Where(m => !m.Deleted)
                    .Where(request.ChannelId.HasValue
                        ? m => m.ChannelId == request.ChannelId
                        : m => m.ConversationId == request.ConversationId);

                if (request.Before.HasValue)
                    query = query.Where(m => m.CreateDate < request.Before.Value);

                // نرمال‌سازی سمت دیتابیس هم انجام می‌شود تا پیام‌هایی که با حروف
                // عربی ذخیره شده‌اند پیدا شوند. چون LIKE با % ابتدایی به‌هرحال
                // ایندکس متنی نمی‌گیرد، این REPLACEها هزینه‌ی محسوسی ندارند.
                query = query.Where(m =>
                    EF.Functions.Like(
                        m.Content!
                            .Replace("\u064A", "\u06CC")
                            .Replace("\u0643", "\u06A9")
                            .Replace("\u200C", ""),
                        pattern, "\\")
                    || m.ChatFiles.Any(f =>
                        EF.Functions.Like(f.FileName!, pattern, "\\")));

                // یکی بیشتر می‌گیریم تا بفهمیم صفحه‌ی بعدی وجود دارد یا نه
                var rows = await query
                    .OrderByDescending(m => m.CreateDate)
                    .Take(take + 1)
                    .Select(m => new SearchHit(
                        m.Id,
                        m.CreateDate,
                        m.Content,
                        m.CreatedByUser!.FirsName + " " + m.CreatedByUser.LastName,
                        m.CreatedByUserId!.Value,
                        m.CreatedByUserId == myId,
                        (int)m.MessageType,
                        m.ChatFiles.Select(f => f.FileName).FirstOrDefault()))
                    .ToListAsync(cancellationToken);

                var hasMore = rows.Count > take;
                if (hasMore) rows.RemoveAt(rows.Count - 1);

                return new SearchMessagesResponse(
                    [.. rows],
                    rows.Count > 0 ? rows[^1].SendAt : null,
                    hasMore);
            }

            private Task<bool> IsMemberAsync(
                SearchMessagesQuery request, Guid myId, CancellationToken ct)
            {
                if (request.ChannelId.HasValue)
                    return _uow.Channels
                        .AnyAsync(c => c.Id == request.ChannelId
                                    && c.Members.Any(u => u.UserId == myId), ct);

                return _uow.Conversation
                    .AnyAsync(c => c.Id == request.ConversationId
                                && c.Users.Any(u => u.UserId == myId), ct);
            }
        }
    }


