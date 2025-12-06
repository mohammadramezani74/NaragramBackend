using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SetpChannelPicChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChannelId",
                table: "ChannelAvatars",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDate",
                table: "ChannelAvatars",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByBrowserName",
                table: "ChannelAvatars",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByIp",
                table: "ChannelAvatars",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "ChannelAvatars",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                table: "ChannelAvatars",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByBrowserName",
                table: "ChannelAvatars",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedById",
                table: "ChannelAvatars",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByIp",
                table: "ChannelAvatars",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "ChannelAvatars",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChannelAvatars_ChannelId",
                table: "ChannelAvatars",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelAvatars_CreatedByUserId",
                table: "ChannelAvatars",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelAvatars_ModifiedById",
                table: "ChannelAvatars",
                column: "ModifiedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelAvatars_AspNetUsers_CreatedByUserId",
                table: "ChannelAvatars",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelAvatars_AspNetUsers_ModifiedById",
                table: "ChannelAvatars",
                column: "ModifiedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelAvatars_Channel_ChannelId",
                table: "ChannelAvatars",
                column: "ChannelId",
                principalTable: "Channel",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChannelAvatars_AspNetUsers_CreatedByUserId",
                table: "ChannelAvatars");

            migrationBuilder.DropForeignKey(
                name: "FK_ChannelAvatars_AspNetUsers_ModifiedById",
                table: "ChannelAvatars");

            migrationBuilder.DropForeignKey(
                name: "FK_ChannelAvatars_Channel_ChannelId",
                table: "ChannelAvatars");

            migrationBuilder.DropIndex(
                name: "IX_ChannelAvatars_ChannelId",
                table: "ChannelAvatars");

            migrationBuilder.DropIndex(
                name: "IX_ChannelAvatars_CreatedByUserId",
                table: "ChannelAvatars");

            migrationBuilder.DropIndex(
                name: "IX_ChannelAvatars_ModifiedById",
                table: "ChannelAvatars");

            migrationBuilder.DropColumn(
                name: "ChannelId",
                table: "ChannelAvatars");

            migrationBuilder.DropColumn(
                name: "CreateDate",
                table: "ChannelAvatars");

            migrationBuilder.DropColumn(
                name: "CreatedByBrowserName",
                table: "ChannelAvatars");

            migrationBuilder.DropColumn(
                name: "CreatedByIp",
                table: "ChannelAvatars");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ChannelAvatars");

            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "ChannelAvatars");

            migrationBuilder.DropColumn(
                name: "ModifiedByBrowserName",
                table: "ChannelAvatars");

            migrationBuilder.DropColumn(
                name: "ModifiedById",
                table: "ChannelAvatars");

            migrationBuilder.DropColumn(
                name: "ModifiedByIp",
                table: "ChannelAvatars");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "ChannelAvatars");
        }
    }
}
