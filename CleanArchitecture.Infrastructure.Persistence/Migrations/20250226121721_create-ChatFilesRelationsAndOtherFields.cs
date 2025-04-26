using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class createChatFilesRelationsAndOtherFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDate",
                table: "ChatFiles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByBrowserName",
                table: "ChatFiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByIp",
                table: "ChatFiles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "ChatFiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                table: "ChatFiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DurationInSeconds",
                table: "ChatFiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MessageId",
                table: "ChatFiles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByBrowserName",
                table: "ChatFiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedById",
                table: "ChatFiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByIp",
                table: "ChatFiles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "ChatFiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatFiles_CreatedByUserId",
                table: "ChatFiles",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatFiles_MessageId",
                table: "ChatFiles",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatFiles_ModifiedById",
                table: "ChatFiles",
                column: "ModifiedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatFiles_AspNetUsers_CreatedByUserId",
                table: "ChatFiles",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatFiles_AspNetUsers_ModifiedById",
                table: "ChatFiles",
                column: "ModifiedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatFiles_Messages_MessageId",
                table: "ChatFiles",
                column: "MessageId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatFiles_AspNetUsers_CreatedByUserId",
                table: "ChatFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatFiles_AspNetUsers_ModifiedById",
                table: "ChatFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatFiles_Messages_MessageId",
                table: "ChatFiles");

            migrationBuilder.DropIndex(
                name: "IX_ChatFiles_CreatedByUserId",
                table: "ChatFiles");

            migrationBuilder.DropIndex(
                name: "IX_ChatFiles_MessageId",
                table: "ChatFiles");

            migrationBuilder.DropIndex(
                name: "IX_ChatFiles_ModifiedById",
                table: "ChatFiles");

            migrationBuilder.DropColumn(
                name: "CreateDate",
                table: "ChatFiles");

            migrationBuilder.DropColumn(
                name: "CreatedByBrowserName",
                table: "ChatFiles");

            migrationBuilder.DropColumn(
                name: "CreatedByIp",
                table: "ChatFiles");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ChatFiles");

            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "ChatFiles");

            migrationBuilder.DropColumn(
                name: "DurationInSeconds",
                table: "ChatFiles");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "ChatFiles");

            migrationBuilder.DropColumn(
                name: "ModifiedByBrowserName",
                table: "ChatFiles");

            migrationBuilder.DropColumn(
                name: "ModifiedById",
                table: "ChatFiles");

            migrationBuilder.DropColumn(
                name: "ModifiedByIp",
                table: "ChatFiles");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "ChatFiles");
        }
    }
}
