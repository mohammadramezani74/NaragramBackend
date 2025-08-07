using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldsLastMessageToConverSation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UnreadCount",
                table: "ConversationUser",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "LastMessageId",
                table: "Conversation",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMessageSentAt",
                table: "Conversation",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastMessageText",
                table: "Conversation",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastUserSenderMessageId",
                table: "Conversation",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnreadCount",
                table: "ConversationUser");

            migrationBuilder.DropColumn(
                name: "LastMessageId",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "LastMessageSentAt",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "LastMessageText",
                table: "Conversation");

            migrationBuilder.DropColumn(
                name: "LastUserSenderMessageId",
                table: "Conversation");
        }
    }
}
