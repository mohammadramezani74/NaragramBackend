using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArchitecture.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateConversationAvatar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
            "Create Table [dbo].[ConversationAvatar] (" +
            "[Id][uniqueidentifier] ROWGUIDCOL  NOT NULL," +
            "[FileName][nvarchar](200) NULL," +
            "[Extension][varchar](10) NULL," +
            "[FileSize][numeric](18, 2) NULL," +
            "[FileData][varbinary](max) FILESTREAM  NULL," +
            "[Thumbnail][varbinary](max) FILESTREAM  NULL," +
            "CONSTRAINT[PK_ConversationAvatar] PRIMARY KEY CLUSTERED(" +
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
                name: "ConversationAvatar");

       
        }
    }
}
