using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdLocationsParameterToMessageEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Latitude",
                table: "Messages",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Longitude",
                table: "Messages",
                type: "real",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Messages");
        }
    }
}
