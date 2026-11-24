using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Purchasing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPurchasing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "purchasing");

            migrationBuilder.CreateTable(
                name: "suppliers",
                schema: "purchasing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    eik = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    payment_term_days = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "deliveries",
                schema: "purchasing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    invoice_date = table.Column<DateOnly>(type: "date", nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    posted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "fk_deliveries_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "purchasing",
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "delivery_lines",
                schema: "purchasing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_factor = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_purchase_price = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    markup_percent = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: true),
                    sale_price = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    batch_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_removed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_lines", x => x.id);
                    table.CheckConstraint("ck_delivery_lines_quantity", "quantity > 0 AND unit_factor >= 1");
                    table.ForeignKey(
                        name: "fk_delivery_lines_deliveries_delivery_id",
                        column: x => x.delivery_id,
                        principalSchema: "purchasing",
                        principalTable: "deliveries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_deliveries_received_at",
                schema: "purchasing",
                table: "deliveries",
                column: "received_at");

            migrationBuilder.CreateIndex(
                name: "ux_deliveries_supplier_id_invoice_no",
                schema: "purchasing",
                table: "deliveries",
                columns: new[] { "supplier_id", "invoice_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_delivery_lines_delivery_id",
                schema: "purchasing",
                table: "delivery_lines",
                column: "delivery_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_lines_product_id",
                schema: "purchasing",
                table: "delivery_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ux_suppliers_eik",
                schema: "purchasing",
                table: "suppliers",
                column: "eik",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_suppliers_name",
                schema: "purchasing",
                table: "suppliers",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "delivery_lines",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "deliveries",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "suppliers",
                schema: "purchasing");
        }
    }
}
