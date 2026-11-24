using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "products",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    atc_code = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    manufacturer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    vat_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    is_rx = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_units",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    factor = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_units", x => x.id);
                    table.CheckConstraint("ck_product_units_factor", "factor >= 1");
                    table.ForeignKey(
                        name: "fk_product_units_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "barcodes",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_barcodes", x => x.id);
                    table.ForeignKey(
                        name: "fk_barcodes_product_units_product_unit_id",
                        column: x => x.product_unit_id,
                        principalSchema: "catalog",
                        principalTable: "product_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_barcodes_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_barcodes_product_id",
                schema: "catalog",
                table: "barcodes",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_barcodes_product_unit_id",
                schema: "catalog",
                table: "barcodes",
                column: "product_unit_id");

            migrationBuilder.CreateIndex(
                name: "ux_barcodes_code",
                schema: "catalog",
                table: "barcodes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_units_product_id_name",
                schema: "catalog",
                table: "product_units",
                columns: new[] { "product_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_product_units_one_base_unit",
                schema: "catalog",
                table: "product_units",
                column: "product_id",
                unique: true,
                filter: "factor = 1");

            migrationBuilder.CreateIndex(
                name: "ix_products_name",
                schema: "catalog",
                table: "products",
                column: "name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "barcodes",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "product_units",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "products",
                schema: "catalog");
        }
    }
}
