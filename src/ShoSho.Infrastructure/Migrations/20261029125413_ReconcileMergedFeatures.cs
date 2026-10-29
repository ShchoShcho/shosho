using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoSho.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileMergedFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Older installations may already have applied the alternate tag migrations.
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Tag" ("Id" TEXT NOT NULL PRIMARY KEY, "Name" TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS "MediaItemTag" ("MediaItemsId" TEXT NOT NULL, "TagsId" TEXT NOT NULL);
                INSERT OR IGNORE INTO "Tags" ("Id", "Name") SELECT "Id", "Name" FROM "Tag";
                INSERT OR IGNORE INTO "ItemTag" ("MediaItemsId", "TagsId")
                    SELECT link."MediaItemsId", canonical."Id" FROM "MediaItemTag" link
                    JOIN "Tags" canonical ON canonical."Id" = link."TagsId";
                INSERT OR IGNORE INTO "ItemTag" ("MediaItemsId", "TagsId")
                    SELECT link."MediaItemsId", canonical."Id" FROM "MediaItemTag" link
                    JOIN "Tag" old ON old."Id" = link."TagsId"
                    JOIN "Tags" canonical ON canonical."Name" = old."Name";
                DROP TABLE "MediaItemTag";
                DROP TABLE "Tag";
                """);

            migrationBuilder.AlterColumn<int>(
                name: "Rating",
                table: "Reviews",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "MediaItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "MediaItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "Книга");
            migrationBuilder.Sql("""
                UPDATE "MediaItems" SET "Status" = CASE "Status"
                    WHEN 'В планах' THEN 'Хочу' WHEN 'В процесі' THEN 'Ін прогрес'
                    WHEN 'Завершено' THEN 'Вже всьо' WHEN 'Відкладено' THEN 'Вагаюсі'
                    ELSE "Status" END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rating",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "MediaItems");

            migrationBuilder.AlterColumn<int>(
                name: "Rating",
                table: "Reviews",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
