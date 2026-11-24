using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogCodesAndMarkups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "product_codes",
                schema: "catalog");

            migrationBuilder.AddColumn<long>(
                name: "code",
                schema: "catalog",
                table: "products",
                type: "bigint",
                nullable: false,
                defaultValueSql: "nextval('catalog.product_codes')");

            migrationBuilder.AddColumn<string>(
                name: "generic_name",
                schema: "catalog",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "latin_name",
                schema: "catalog",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "markup_percent",
                schema: "catalog",
                table: "products",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_stock",
                schema: "catalog",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "min_stock",
                schema: "catalog",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "markup_percent",
                schema: "catalog",
                table: "product_groups",
                type: "numeric(7,2)",
                precision: 7,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_code",
                schema: "catalog",
                table: "products",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_products_code",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "code",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "generic_name",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "latin_name",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "markup_percent",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "max_stock",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "min_stock",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "markup_percent",
                schema: "catalog",
                table: "product_groups");

            migrationBuilder.DropSequence(
                name: "product_codes",
                schema: "catalog");
        }
    }
}
