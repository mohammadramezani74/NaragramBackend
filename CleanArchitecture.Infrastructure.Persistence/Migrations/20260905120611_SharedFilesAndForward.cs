using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SharedFilesAndForward : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ترتیب اینجا عمدی است و نسبت به چیزی که EF تولید کرده عوض شده:
            // ابتدا جدول واسط ساخته و پر می‌شود، و فقط بعد از آن ستون قدیمی
            // حذف می‌گردد. در نسخه‌ی تولیدشده ستون اول حذف می‌شد و جدول خالی
            // می‌ماند، یعنی ارتباط همه‌ی فایل‌های موجود با پیام‌هایشان از بین
            // می‌رفت و فایل‌های قدیمی از چت‌ها ناپدید می‌شدند.

            migrationBuilder.AddColumn<string>(
                name: "ForwardedFromName",
                table: "Messages",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MessageFiles",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChatFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageFiles", x => new { x.MessageId, x.ChatFileId });
                    table.ForeignKey(
                        name: "FK_MessageFiles_ChatFiles_ChatFileId",
                        column: x => x.ChatFileId,
                        principalTable: "ChatFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessageFiles_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessageFiles_ChatFileId",
                table: "MessageFiles",
                column: "ChatFileId");

            // انتقال داده‌ی موجود. فقط شناسه‌ها کپی می‌شوند، نه بایت فایل‌ها،
            // پس سریع است. شرط EXISTS برای این است که اگر ردیف یتیمی وجود
            // داشته باشد (فایلی که پیامش قبلاً پاک شده) مایگریشن با خطای کلید
            // خارجی نخوابد.
            migrationBuilder.Sql(@"
                INSERT INTO [MessageFiles] ([MessageId], [ChatFileId], [CreateDate])
                SELECT f.[MessageId], f.[Id], ISNULL(f.[CreateDate], SYSDATETIME())
                FROM [ChatFiles] AS f
                WHERE EXISTS (SELECT 1 FROM [Messages] AS m WHERE m.[Id] = f.[MessageId]);
            ");

            // حالا که داده منتقل شده، ستون قدیمی می‌رود.
            migrationBuilder.DropForeignKey(
                name: "FK_ChatFiles_Messages_MessageId",
                table: "ChatFiles");

            migrationBuilder.DropIndex(
                name: "IX_ChatFiles_MessageId",
                table: "ChatFiles");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "ChatFiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // برگشت هم باید داده را برگرداند، وگرنه با rollback همه‌ی فایل‌ها
            // MessageId خالی می‌گیرند. اگر فایلی به چند پیام وصل شده باشد
            // (فوروارد شده) فقط یکی از آن‌ها قابل بازگردانی است — این محدودیت
            // ذاتی مدل قدیمی است.
            migrationBuilder.AddColumn<Guid>(
                name: "MessageId",
                table: "ChatFiles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(@"
                UPDATE f
                SET f.[MessageId] = x.[MessageId]
                FROM [ChatFiles] AS f
                CROSS APPLY (
                    SELECT TOP 1 mf.[MessageId]
                    FROM [MessageFiles] AS mf
                    WHERE mf.[ChatFileId] = f.[Id]
                    ORDER BY mf.[CreateDate]
                ) AS x;
            ");

            migrationBuilder.DropTable(
                name: "MessageFiles");

            migrationBuilder.DropColumn(
                name: "ForwardedFromName",
                table: "Messages");

            migrationBuilder.CreateIndex(
                name: "IX_ChatFiles_MessageId",
                table: "ChatFiles",
                column: "MessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatFiles_Messages_MessageId",
                table: "ChatFiles",
                column: "MessageId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}