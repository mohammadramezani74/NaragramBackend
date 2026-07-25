using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SetupConversationAvatar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                table: "ConversationAvatar",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDate",
                table: "ConversationAvatar",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByBrowserName",
                table: "ConversationAvatar",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByIp",
                table: "ConversationAvatar",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "ConversationAvatar",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                table: "ConversationAvatar",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByBrowserName",
                table: "ConversationAvatar",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedById",
                table: "ConversationAvatar",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByIp",
                table: "ConversationAvatar",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "ConversationAvatar",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConversationAvatar_ConversationId",
                table: "ConversationAvatar",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationAvatar_CreatedByUserId",
                table: "ConversationAvatar",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationAvatar_ModifiedById",
                table: "ConversationAvatar",
                column: "ModifiedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationAvatar_AspNetUsers_CreatedByUserId",
                table: "ConversationAvatar",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationAvatar_AspNetUsers_ModifiedById",
                table: "ConversationAvatar",
                column: "ModifiedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationAvatar_Conversation_ConversationId",
                table: "ConversationAvatar",
                column: "ConversationId",
                principalTable: "Conversation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConversationAvatar_AspNetUsers_CreatedByUserId",
                table: "ConversationAvatar");

            migrationBuilder.DropForeignKey(
                name: "FK_ConversationAvatar_AspNetUsers_ModifiedById",
                table: "ConversationAvatar");

            migrationBuilder.DropForeignKey(
                name: "FK_ConversationAvatar_Conversation_ConversationId",
                table: "ConversationAvatar");

            migrationBuilder.DropIndex(
                name: "IX_ConversationAvatar_ConversationId",
                table: "ConversationAvatar");

            migrationBuilder.DropIndex(
                name: "IX_ConversationAvatar_CreatedByUserId",
                table: "ConversationAvatar");

            migrationBuilder.DropIndex(
                name: "IX_ConversationAvatar_ModifiedById",
                table: "ConversationAvatar");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "ConversationAvatar");

            migrationBuilder.DropColumn(
                name: "CreateDate",
                table: "ConversationAvatar");

            migrationBuilder.DropColumn(
                name: "CreatedByBrowserName",
                table: "ConversationAvatar");

            migrationBuilder.DropColumn(
                name: "CreatedByIp",
                table: "ConversationAvatar");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ConversationAvatar");

            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "ConversationAvatar");

            migrationBuilder.DropColumn(
                name: "ModifiedByBrowserName",
                table: "ConversationAvatar");

            migrationBuilder.DropColumn(
                name: "ModifiedById",
                table: "ConversationAvatar");

            migrationBuilder.DropColumn(
                name: "ModifiedByIp",
                table: "ConversationAvatar");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "ConversationAvatar");
        }
    }
}
