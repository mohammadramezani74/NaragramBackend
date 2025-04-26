using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CAddSomeNavigationToUserAvatars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreateDate",
                table: "UserAvatars",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByBrowserName",
                table: "UserAvatars",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByIp",
                table: "UserAvatars",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "UserAvatars",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                table: "UserAvatars",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByBrowserName",
                table: "UserAvatars",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedById",
                table: "UserAvatars",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedByIp",
                table: "UserAvatars",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "UserAvatars",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "UserAvatars",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_UserAvatars_CreatedByUserId",
                table: "UserAvatars",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAvatars_ModifiedById",
                table: "UserAvatars",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_UserAvatars_UserId",
                table: "UserAvatars",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAvatars_AspNetUsers_CreatedByUserId",
                table: "UserAvatars",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAvatars_AspNetUsers_ModifiedById",
                table: "UserAvatars",
                column: "ModifiedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAvatars_AspNetUsers_UserId",
                table: "UserAvatars",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAvatars_AspNetUsers_CreatedByUserId",
                table: "UserAvatars");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAvatars_AspNetUsers_ModifiedById",
                table: "UserAvatars");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAvatars_AspNetUsers_UserId",
                table: "UserAvatars");

            migrationBuilder.DropIndex(
                name: "IX_UserAvatars_CreatedByUserId",
                table: "UserAvatars");

            migrationBuilder.DropIndex(
                name: "IX_UserAvatars_ModifiedById",
                table: "UserAvatars");

            migrationBuilder.DropIndex(
                name: "IX_UserAvatars_UserId",
                table: "UserAvatars");

            migrationBuilder.DropColumn(
                name: "CreateDate",
                table: "UserAvatars");

            migrationBuilder.DropColumn(
                name: "CreatedByBrowserName",
                table: "UserAvatars");

            migrationBuilder.DropColumn(
                name: "CreatedByIp",
                table: "UserAvatars");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "UserAvatars");

            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "UserAvatars");

            migrationBuilder.DropColumn(
                name: "ModifiedByBrowserName",
                table: "UserAvatars");

            migrationBuilder.DropColumn(
                name: "ModifiedById",
                table: "UserAvatars");

            migrationBuilder.DropColumn(
                name: "ModifiedByIp",
                table: "UserAvatars");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "UserAvatars");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserAvatars");
        }
    }
}
