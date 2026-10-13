using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PrivateConversationPairIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserAId",
                table: "Conversation",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserBId",
                table: "Conversation",
                type: "uniqueidentifier",
                nullable: true);

            // پر کردن ستون‌ها برای گفتگوهای موجود، قبل از ساخت ایندکس.
            //
            // بدون این، همه‌ی گفتگوهای خصوصی کلید خالی می‌مانند و کد جدید
            // هیچ‌کدامشان را پیدا نمی‌کند؛ یعنی برای هر مخاطب یک گفتگوی تازه
            // می‌سازد و همان تکراری‌هایی که تازه پاک شدند از نو ساخته می‌شوند.
            //
            // ترتیب مرتب‌شده (کوچک‌تر در A) همان قراردادی است که Conversation.Create
            // رعایت می‌کند، تا «الف با ب» و «ب با الف» یک کلید بدهند.
            migrationBuilder.Sql(@"
                WITH Pair AS (
                    SELECT cu.ConversationId,
                           MIN(CONVERT(varchar(36), cu.UserId)) AS A,
                           MAX(CONVERT(varchar(36), cu.UserId)) AS B,
                           COUNT(*) AS MemberCount
                    FROM [ConversationUser] cu
                    GROUP BY cu.ConversationId
                )
                UPDATE c
                SET c.[UserAId] = CONVERT(uniqueidentifier, p.A),
                    c.[UserBId] = CONVERT(uniqueidentifier, p.B)
                FROM [Conversation] c
                JOIN Pair p ON p.ConversationId = c.Id
                WHERE c.[IsPrivate] = 1
                  AND p.MemberCount = 2;
            ");

            migrationBuilder.CreateIndex(
                name: "UX_Conversations_PrivatePair",
                table: "Conversation",
                columns: new[] { "UserAId", "UserBId" },
                unique: true,
                filter: "[IsPrivate] = 1 AND [UserAId] IS NOT NULL AND [UserBId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Conversations_PrivatePair",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "UserAId",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "UserBId",
                table: "Conversation");
        }
    }
}