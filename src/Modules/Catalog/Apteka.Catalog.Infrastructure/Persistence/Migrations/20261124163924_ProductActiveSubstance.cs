using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductActiveSubstance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "active_substance",
                schema: "catalog",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "active_substance",
                schema: "catalog",
                table: "products");
        }
    }
}
