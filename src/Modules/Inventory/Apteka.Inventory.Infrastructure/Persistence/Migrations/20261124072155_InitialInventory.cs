using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.CreateTable(
                name: "batches",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    stock_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    purchase_price = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    sale_price = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    priced_unit_factor = table.Column<int>(type: "integer", nullable: false),
                    priced_unit_sale_price = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    source_document_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    source_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_batches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_delta = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    document_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_movements_batches_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "inventory",
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_batches_product_id_expiry_date",
                schema: "inventory",
                table: "batches",
                columns: new[] { "product_id", "expiry_date" });

            migrationBuilder.CreateIndex(
                name: "ux_batches_source_line_id",
                schema: "inventory",
                table: "batches",
                column: "source_line_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_batch_id",
                schema: "inventory",
                table: "stock_movements",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_document_type_document_id",
                schema: "inventory",
                table: "stock_movements",
                columns: new[] { "document_type", "document_id" });

            // Stock movements are append-only (rule 3): a mistake is fixed with a new, opposite movement.
            migrationBuilder.Sql("""
                CREATE FUNCTION inventory.reject_stock_movement_change() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'inventory.stock_movements is append-only (% is not allowed)', TG_OP;
                END;
                $$;

                CREATE TRIGGER stock_movements_append_only
                BEFORE UPDATE OR DELETE ON inventory.stock_movements
                FOR EACH ROW EXECUTE FUNCTION inventory.reject_stock_movement_change();

                CREATE TRIGGER stock_movements_no_truncate
                BEFORE TRUNCATE ON inventory.stock_movements
                FOR EACH STATEMENT EXECUTE FUNCTION inventory.reject_stock_movement_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS stock_movements_no_truncate ON inventory.stock_movements;
                DROP TRIGGER IF EXISTS stock_movements_append_only ON inventory.stock_movements;
                DROP FUNCTION IF EXISTS inventory.reject_stock_movement_change();
                """);

            migrationBuilder.DropTable(
                name: "stock_movements",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "batches",
                schema: "inventory");
        }
    }
}
