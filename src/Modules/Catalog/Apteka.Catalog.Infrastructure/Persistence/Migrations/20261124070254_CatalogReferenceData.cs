using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogReferenceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "default_sale_unit_id",
                schema: "catalog",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dispensing_mode",
                schema: "catalog",
                table: "products",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "OverTheCounter");

            migrationBuilder.AddColumn<Guid>(
                name: "manufacturer_id",
                schema: "catalog",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nhif_code",
                schema: "catalog",
                table: "products",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_group_id",
                schema: "catalog",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "shelf_id",
                schema: "catalog",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "manufacturers",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_manufacturers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_groups",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shelves",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shelves", x => x.id);
                });

            // Keep existing data: is_rx becomes the dispensing mode, and the manufacturer text
            // becomes a row in catalog.manufacturers that the product points to.
            migrationBuilder.Sql("""
                UPDATE catalog.products SET dispensing_mode = 'Prescription' WHERE is_rx;

                INSERT INTO catalog.manufacturers (id, name, is_active)
                SELECT gen_random_uuid(), m.name, TRUE
                FROM (SELECT DISTINCT btrim(manufacturer) AS name
                      FROM catalog.products
                      WHERE manufacturer IS NOT NULL AND btrim(manufacturer) <> '') AS m;

                UPDATE catalog.products AS p
                SET manufacturer_id = m.id
                FROM catalog.manufacturers AS m
                WHERE m.name = btrim(p.manufacturer);
                """);

            migrationBuilder.DropColumn(
                name: "is_rx",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "manufacturer",
                schema: "catalog",
                table: "products");

            migrationBuilder.CreateIndex(
                name: "ix_products_manufacturer_id",
                schema: "catalog",
                table: "products",
                column: "manufacturer_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_nhif_code",
                schema: "catalog",
                table: "products",
                column: "nhif_code");

            migrationBuilder.CreateIndex(
                name: "ix_products_product_group_id",
                schema: "catalog",
                table: "products",
                column: "product_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_shelf_id",
                schema: "catalog",
                table: "products",
                column: "shelf_id");

            migrationBuilder.CreateIndex(
                name: "ux_manufacturers_name",
                schema: "catalog",
                table: "manufacturers",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_product_groups_name",
                schema: "catalog",
                table: "product_groups",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_shelves_name",
                schema: "catalog",
                table: "shelves",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_products_manufacturers_manufacturer_id",
                schema: "catalog",
                table: "products",
                column: "manufacturer_id",
                principalSchema: "catalog",
                principalTable: "manufacturers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_product_groups_product_group_id",
                schema: "catalog",
                table: "products",
                column: "product_group_id",
                principalSchema: "catalog",
                principalTable: "product_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_shelves_shelf_id",
                schema: "catalog",
                table: "products",
                column: "shelf_id",
                principalSchema: "catalog",
                principalTable: "shelves",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_products_manufacturers_manufacturer_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_products_product_groups_product_group_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "fk_products_shelves_shelf_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropTable(
                name: "manufacturers",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "product_groups",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "shelves",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "ix_products_manufacturer_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_nhif_code",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_product_group_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_shelf_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "default_sale_unit_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "dispensing_mode",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "manufacturer_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "nhif_code",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "product_group_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "shelf_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.AddColumn<bool>(
                name: "is_rx",
                schema: "catalog",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "manufacturer",
                schema: "catalog",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
