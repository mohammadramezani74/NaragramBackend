using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.Messaging;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Application.Common.Utilities.Text;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Chats.Messages.Query.SearchMessages
{
    /// <summary>
    /// متن پیام‌های جدید در دیتابیس رمزشده است و SQL نمی‌تواند داخلش را بگردد.
    /// پس پیام‌های گفتگو را از جدید به قدیم دسته‌دسته می‌خوانیم (EF خودش رمز را
    /// باز می‌کند) و در حافظه مقایسه می‌کنیم. پیام‌های قدیمیِ رمزنشده هم از همین
    /// مسیر پیدا می‌شوند. نام فایل‌ها رمز نشده و همچنان در SQL جستجو می‌شود.
    ///
    /// برای اینکه یک گفتگوی خیلی بزرگ سرور را درگیر نکند، هر درخواست سقف
    /// پیمایش دارد؛ اگر به سقف برسد HasMore می‌ماند و کلاینت ادامه می‌دهد.
    /// </summary>
    internal sealed class SearchMessagesQueryHandler(
            IApplicationUnitOfWork uow,
            IApplicationUserManager userManager)
            : IQueryHandler<SearchMessagesQuery, SearchMessagesResponse>
        {
            private readonly IApplicationUnitOfWork _uow = uow;
            private readonly IApplicationUserManager _userManager = userManager;

            private const int MinTermLength = 2;
            private const int MaxTake = 50;
            private const int BatchSize = 500;

            // تا وقتی هیچ نتیجه‌ای نیافته‌ایم بیشتر می‌گردیم تا کاربر «نتیجه‌ای
            // یافت نشد» اشتباهی نبیند؛ بعد از اولین نتیجه زودتر برمی‌گردیم.
            private const int MaxScanNoHits = 20_000;
            private const int MaxScanWithHits = 5_000;

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

                var scope = _uow.Messages.AsNoTracking()
                    .Where(m => !m.Deleted)
                    .Where(request.ChannelId.HasValue
                        ? m => m.ChannelId == request.ChannelId
                        : m => m.ConversationId == request.ConversationId);

                var hits = new List<SearchHit>(take + 1);
                var cursor = request.Before;
                var scanned = 0;
                var exhausted = false;

                while (hits.Count <= take
                       && scanned < (hits.Count == 0 ? MaxScanNoHits : MaxScanWithHits))
                {
                    var query = scope;
                    if (cursor.HasValue)
                    {
                        var before = cursor.Value;
                        query = query.Where(m => m.CreateDate < before);
                    }

                    var batch = await query
                        .OrderByDescending(m => m.CreateDate)
                        .Take(BatchSize)
                        .Select(m => new Row(
                            m.Id,
                            m.CreateDate,
                            m.Content,
                            m.CreatedByUser!.FirsName + " " + m.CreatedByUser.LastName,
                            m.CreatedByUserId!.Value,
                            (int)m.MessageType,
                            m.ChatFiles.Select(f => f.FileName).FirstOrDefault(),
                            m.ChatFiles.Any(f => EF.Functions.Like(f.FileName!, pattern, "\\"))))
                        .ToListAsync(cancellationToken);

                    scanned += batch.Count;

                    foreach (var r in batch)
                    {
                        if (!r.FileMatch && !Matches(r.Content, term))
                            continue;

                        hits.Add(new SearchHit(
                            r.Id, r.CreateDate, r.Content, r.SenderName, r.SenderId,
                            r.SenderId == myId, r.Type, r.FileName));

                        // یکی بیشتر برای فهمیدن وجود صفحه‌ی بعد
                        if (hits.Count > take) break;
                    }

                    if (batch.Count < BatchSize)
                    {
                        exhausted = true;
                        break;
                    }

                    cursor = batch[^1].CreateDate;
                }

                if (hits.Count > take)
                {
                    hits.RemoveAt(hits.Count - 1);
                    return new SearchMessagesResponse([.. hits], hits[^1].SendAt, true);
                }

                // به سقف پیمایش رسیدیم ولی پیام‌های قدیمی‌تر هنوز مانده
                if (!exhausted)
                    return new SearchMessagesResponse([.. hits], cursor, true);

                return new SearchMessagesResponse(
                    [.. hits],
                    hits.Count > 0 ? hits[^1].SendAt : null,
                    false);
            }

            private static bool Matches(string? content, string term) =>
                !string.IsNullOrEmpty(content)
                && PersianText.Normalize(content).Contains(term, StringComparison.OrdinalIgnoreCase);

            private sealed record Row(
                Guid Id,
                DateTime CreateDate,
                string? Content,
                string SenderName,
                Guid SenderId,
                int Type,
                string? FileName,
                bool FileMatch);

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
