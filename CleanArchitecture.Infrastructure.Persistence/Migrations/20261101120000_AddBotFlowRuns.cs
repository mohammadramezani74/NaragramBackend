using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Persistence.Migrations
{
    /// <summary>جدول تازه برای ثبت اجرای جریان‌های ربات (گزارش ربات‌ها). داده‌ی قبلی دست نمی‌خورد.</summary>
    public partial class AddBotFlowRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BotFlowRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlowKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotFlowRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BotFlowRuns_Bots_BotId",
                        column: x => x.BotId,
                        principalTable: "Bots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BotFlowRuns_BotId_UserId_StartedAt",
                table: "BotFlowRuns",
                columns: new[] { "BotId", "UserId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BotFlowRuns_StartedAt",
                table: "BotFlowRuns",
                column: "StartedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BotFlowRuns");
        }
    }
}
