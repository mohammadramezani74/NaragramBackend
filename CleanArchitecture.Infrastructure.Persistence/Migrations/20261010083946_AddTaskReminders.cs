using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RemindAtUtc",
                table: "ContactTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderSentAtUtc",
                table: "ContactTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactTasks_PendingReminders",
                table: "ContactTasks",
                column: "RemindAtUtc",
                filter: "[RemindAtUtc] IS NOT NULL AND [ReminderSentAtUtc] IS NULL AND [IsDone] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ContactTasks_PendingReminders",
                table: "ContactTasks");

            migrationBuilder.DropColumn(
                name: "RemindAtUtc",
                table: "ContactTasks");

            migrationBuilder.DropColumn(
                name: "ReminderSentAtUtc",
                table: "ContactTasks");
        }
    }
}
