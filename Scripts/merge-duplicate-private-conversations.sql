/*
    ادغام گفتگوهای خصوصی تکراری، قبل از migration «PrivateConversationPairIndex».

    آن migration ایندکس یکتای UX_Conversations_PrivatePair را روی جفت کاربرهای هر
    گفتگوی خصوصی می‌سازد؛ اگر دو نفر بیش از یک گفتگوی خصوصی داشته باشند، با خطای
    «duplicate key» متوقف می‌شود. این اسکریپت برای هر جفت، گفتگوی پرپیام‌تر (در تساوی،
    قدیمی‌تر) را نگه می‌دارد و پیام‌های بقیه را به آن منتقل می‌کند؛ هیچ پیامی پاک نمی‌شود.
    بعد گفتگوهای اضافی (با اعضا و عکسشان) حذف می‌شوند.

    کلید جفت دقیقاً همان است که migration حساب می‌کند (کمترین و بیشترین UserId به صورت
    متن، فقط گفتگوهای خصوصی با دقیقاً دو ردیف عضو)، تا بعد از این اسکریپت ایندکس ساخته شود.

    روش اجرا:
      1. بکاپ کامل از همین دیتابیس.
      2. اجرا با @DryRun = 1 (پیش‌فرض): فقط گزارش می‌دهد، چیزی عوض نمی‌شود.
      3. اگر گزارش درست بود، @DryRun = 0 و اجرای دوباره (همه در یک تراکنش).
      4. Update-Database یا اجرای برنامه.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @DryRun bit = 1;

PRINT N'Database: ' + DB_NAME() + N' on ' + @@SERVERNAME;

IF OBJECT_ID('tempdb..#Pair') IS NOT NULL DROP TABLE #Pair;
IF OBJECT_ID('tempdb..#Rank') IS NOT NULL DROP TABLE #Rank;
IF OBJECT_ID('tempdb..#Merge') IS NOT NULL DROP TABLE #Merge;

-- ---------------------------------------------------------------- جفت هر گفتگوی خصوصی
SELECT cu.ConversationId,
       MIN(CONVERT(varchar(36), cu.UserId)) AS A,
       MAX(CONVERT(varchar(36), cu.UserId)) AS B
INTO #Pair
FROM dbo.ConversationUser cu
JOIN dbo.Conversation c ON c.Id = cu.ConversationId
WHERE c.IsPrivate = 1
GROUP BY cu.ConversationId
HAVING COUNT(*) = 2;

-- ---------------------------------------------------------------- نگه‌داشتنی: پرپیام‌تر، بعد قدیمی‌تر
;WITH Counted AS (
    SELECT p.ConversationId, p.A, p.B, c.CreateDate,
           (SELECT COUNT(*) FROM dbo.Messages m WHERE m.ConversationId = p.ConversationId) AS Msgs
    FROM #Pair p
    JOIN dbo.Conversation c ON c.Id = p.ConversationId
)
SELECT ConversationId, A, B, Msgs, CreateDate,
       ROW_NUMBER() OVER (PARTITION BY A, B ORDER BY Msgs DESC, CreateDate ASC, ConversationId) AS rn
INTO #Rank
FROM Counted;

SELECT l.ConversationId AS LoserId,
       k.ConversationId AS KeeperId,
       l.A, l.B,
       l.Msgs AS LoserMessages,
       k.Msgs AS KeeperMessages
INTO #Merge
FROM #Rank l
JOIN #Rank k ON k.A = l.A AND k.B = l.B AND k.rn = 1
WHERE l.rn > 1;

-- ---------------------------------------------------------------- گزارش
SELECT COUNT(DISTINCT KeeperId)          AS DuplicatePairs,
       COUNT(*)                          AS ConversationsToRemove,
       ISNULL(SUM(LoserMessages), 0)     AS MessagesToMove
FROM #Merge;

SELECT TOP 100 m.*, ua.UserName AS UserA, ub.UserName AS UserB
FROM #Merge m
LEFT JOIN dbo.AspNetUsers ua ON CONVERT(varchar(36), ua.Id) = m.A
LEFT JOIN dbo.AspNetUsers ub ON CONVERT(varchar(36), ub.Id) = m.B
ORDER BY m.LoserMessages DESC;

IF @DryRun = 1
BEGIN
    PRINT N'DryRun: nothing changed. Check the report, back up, then set @DryRun = 0.';
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM #Merge)
BEGIN
    PRINT N'No duplicates. Nothing to do.';
    RETURN;
END;

-- ---------------------------------------------------------------- اجرا
BEGIN TRAN;

    -- پیام‌ها به گفتگوی نگه‌داشتنی می‌روند (فایل‌ها، واکنش‌ها و پاسخ‌ها به Id پیام وصل‌اند و دست نمی‌خورند)
    UPDATE m
    SET m.ConversationId = x.KeeperId
    FROM dbo.Messages m
    JOIN #Merge x ON x.LoserId = m.ConversationId;

    -- شمار نخوانده‌ی هر نفر جمع می‌شود
    UPDATE ku
    SET ku.UnreadCount = ku.UnreadCount + s.Extra
    FROM dbo.ConversationUser ku
    JOIN (
        SELECT x.KeeperId, lu.UserId, SUM(lu.UnreadCount) AS Extra
        FROM #Merge x
        JOIN dbo.ConversationUser lu ON lu.ConversationId = x.LoserId
        GROUP BY x.KeeperId, lu.UserId
    ) s ON s.KeeperId = ku.ConversationId AND s.UserId = ku.UserId;

    DELETE ca FROM dbo.ConversationAvatar ca JOIN #Merge x ON x.LoserId = ca.ConversationId;
    DELETE cu FROM dbo.ConversationUser cu JOIN #Merge x ON x.LoserId = cu.ConversationId;
    DELETE c  FROM dbo.Conversation c JOIN #Merge x ON x.LoserId = c.Id;

    -- آخرین پیام گفتگوهای نگه‌داشتنی، اگر پیام منتقل‌شده جدیدتر است
    UPDATE c
    SET c.LastMessageId = lm.Id,
        c.LastMessageSentAt = lm.CreateDate,
        c.LastUserSenderMessageId = lm.CreatedByUserId,
        c.LastMessageText = LEFT(CASE WHEN NULLIF(LTRIM(RTRIM(lm.Content)), N'') IS NULL THEN N'فایل' ELSE lm.Content END, 100)
    FROM dbo.Conversation c
    CROSS APPLY (
        SELECT TOP 1 m.Id, m.CreateDate, m.CreatedByUserId, m.Content
        FROM dbo.Messages m
        WHERE m.ConversationId = c.Id AND m.Deleted = 0
        ORDER BY m.CreateDate DESC
    ) lm
    WHERE c.Id IN (SELECT KeeperId FROM #Merge)
      AND lm.CreateDate > ISNULL(c.LastMessageSentAt, '19000101');

COMMIT;

PRINT N'Done. Now run Update-Database (or start the app).';
