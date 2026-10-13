using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContactTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DueDate = table.Column<DateTime>(type: "date", nullable: true),
                    IsDone = table.Column<bool>(type: "bit", nullable: false),
                    DoneAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    SourceMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourcePreview = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SourceSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByBrowserName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByIp = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ModifiedByBrowserName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ModifiedByIp = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactTasks_AspNetUsers_ContactId",
                        column: x => x.ContactId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContactTasks_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContactTasks_AspNetUsers_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContactTasks_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactTasks_ContactId",
                table: "ContactTasks",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactTasks_CreatedByUserId",
                table: "ContactTasks",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactTasks_ModifiedById",
                table: "ContactTasks",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_ContactTasks_Owner_Contact_Done",
                table: "ContactTasks",
                columns: new[] { "OwnerId", "ContactId", "IsDone" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactTasks");
        }
    }
}
