using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoSho.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTagUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS IX_Tags_Name ON Tags (Name);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tags_Name",
                table: "Tags");
        }
    }
}
