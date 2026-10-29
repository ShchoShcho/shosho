using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoSho.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileInitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Schema already created by the restored initial and tag migrations.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This migration does not own the existing tables.
        }
    }
}
