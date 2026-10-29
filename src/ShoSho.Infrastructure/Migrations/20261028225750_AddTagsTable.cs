using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoSho.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTagsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Merge the alternate branch's tag tables without losing assignments.
            migrationBuilder.Sql("""
                INSERT OR IGNORE INTO "Tags" ("Id", "Name") SELECT "Id", "Name" FROM "Tag";
                INSERT OR IGNORE INTO "ItemTag" ("MediaItemsId", "TagsId")
                    SELECT link."MediaItemsId", canonical."Id"
                    FROM "MediaItemTag" link JOIN "Tag" old ON old."Id" = link."TagsId"
                    JOIN "Tags" canonical ON canonical."Name" = old."Name";
                DROP TABLE "MediaItemTag";
                DROP TABLE "Tag";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new System.NotSupportedException("The merged tag schema cannot be downgraded safely.");
        }
    }
}
