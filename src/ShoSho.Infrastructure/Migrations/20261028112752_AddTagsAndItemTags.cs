using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoSho.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTagsAndItemTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Tags" ("Id" TEXT NOT NULL PRIMARY KEY, "Name" TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS "ItemTag" (
                    "MediaItemsId" TEXT NOT NULL REFERENCES "MediaItems"("Id") ON DELETE CASCADE,
                    "TagsId" TEXT NOT NULL REFERENCES "Tags"("Id") ON DELETE CASCADE,
                    PRIMARY KEY ("MediaItemsId", "TagsId"));
                CREATE INDEX IF NOT EXISTS "IX_ItemTag_TagsId" ON "ItemTag" ("TagsId");
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Tags_Name" ON "Tags" ("Name");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemTag");

            migrationBuilder.DropTable(
                name: "Tags");
        }
    }
}
