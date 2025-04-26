using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateChatFilesTbl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.CreateTable(
            //    name: "ChatFiles",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            //        FileData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
            //        Thumbnail = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
            //        FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
            //        Extension = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
            //        FileSize = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_ChatFiles", x => x.Id);
            //    });
            migrationBuilder.Sql(
           "Create Table [dbo].[ChatFiles] (" +
           "[Id][uniqueidentifier] ROWGUIDCOL  NOT NULL," +
           "[FileName][nvarchar](200) NULL," +
           "[Extension][varchar](10) NULL," +
           "[FileSize][numeric](18, 2) NULL," +
           "[FileData][varbinary](max) FILESTREAM  NULL," +
           "[Thumbnail][varbinary](max) FILESTREAM  NULL," +
           "CONSTRAINT[PK_VacationImages] PRIMARY KEY CLUSTERED(" +
           "[Id] ASC" +
           ")WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY =" +
           " OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]" +
           ") ON[PRIMARY]"
       );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatFiles");
        }
    }
}
