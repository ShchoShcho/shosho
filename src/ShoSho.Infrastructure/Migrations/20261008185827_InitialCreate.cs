using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoSho.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Also accepts databases created by the later, duplicate InitialCreate.
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "MediaItems" (
                    "Id" TEXT NOT NULL PRIMARY KEY, "Title" TEXT NOT NULL,
                    "Description" TEXT NOT NULL, "Status" TEXT NOT NULL,
                    "PosterUrl" TEXT NULL, "CreatedAt" TEXT NOT NULL);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaItems");
        }
    }
}
