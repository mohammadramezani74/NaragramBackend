using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMessagePin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "Messages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PinnedAt",
                table: "Messages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PinnedByUserId",
                table: "Messages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChannelId_IsPinned",
                table: "Messages",
                columns: new[] { "ChannelId", "IsPinned" },
                filter: "[IsPinned] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId_IsPinned",
                table: "Messages",
                columns: new[] { "ConversationId", "IsPinned" },
                filter: "[IsPinned] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_ChannelId_IsPinned",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ConversationId_IsPinned",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "PinnedAt",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "PinnedByUserId",
                table: "Messages");
        }
    }
}
