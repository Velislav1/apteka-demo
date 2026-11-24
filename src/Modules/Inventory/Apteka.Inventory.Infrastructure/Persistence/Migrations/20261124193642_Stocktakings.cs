using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Stocktakings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "stocktaking_numbers",
                schema: "inventory");

            migrationBuilder.CreateTable(
                name: "stocktakings",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('inventory.stocktaking_numbers')"),
                    scope = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    started_by = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stocktakings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stocktaking_lines",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stocktaking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expected_quantity = table.Column<int>(type: "integer", nullable: false),
                    counted_quantity = table.Column<int>(type: "integer", nullable: true),
                    difference = table.Column<int>(type: "integer", nullable: true),
                    purchase_price = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stocktaking_lines", x => x.id);
                    table.CheckConstraint("ck_stocktaking_lines_counted", "counted_quantity IS NULL OR counted_quantity >= 0");
                    table.ForeignKey(
                        name: "fk_stocktaking_lines_batches_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "inventory",
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stocktaking_lines_stocktakings_stocktaking_id",
                        column: x => x.stocktaking_id,
                        principalSchema: "inventory",
                        principalTable: "stocktakings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stocktaking_lines_batch_id",
                schema: "inventory",
                table: "stocktaking_lines",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_stocktaking_lines_product_id",
                schema: "inventory",
                table: "stocktaking_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_stocktaking_lines_stocktaking_id",
                schema: "inventory",
                table: "stocktaking_lines",
                column: "stocktaking_id");

            migrationBuilder.CreateIndex(
                name: "ix_stocktakings_number",
                schema: "inventory",
                table: "stocktakings",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_stocktakings_one_open",
                schema: "inventory",
                table: "stocktakings",
                column: "status",
                unique: true,
                filter: "status = 'Open'");

            // Rule 6: a stocktaking is never deleted, and once closed neither it nor its lines change.
            migrationBuilder.Sql("""
                CREATE FUNCTION inventory.protect_stocktaking() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION 'inventory.stocktakings rows are never deleted';
                    END IF;
                    IF OLD.status = 'Closed' THEN
                        RAISE EXCEPTION 'stocktaking % is closed and cannot change', OLD.number;
                    END IF;
                    RETURN NEW;
                END;
                $$;

                CREATE FUNCTION inventory.protect_stocktaking_line() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION 'inventory.stocktaking_lines rows are never deleted';
                    END IF;
                    IF EXISTS (SELECT 1 FROM inventory.stocktakings s WHERE s.id = OLD.stocktaking_id AND s.status = 'Closed') THEN
                        RAISE EXCEPTION 'the stocktaking of this line is closed and cannot change';
                    END IF;
                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER stocktakings_protect BEFORE UPDATE OR DELETE ON inventory.stocktakings
                    FOR EACH ROW EXECUTE FUNCTION inventory.protect_stocktaking();
                CREATE TRIGGER stocktaking_lines_protect BEFORE UPDATE OR DELETE ON inventory.stocktaking_lines
                    FOR EACH ROW EXECUTE FUNCTION inventory.protect_stocktaking_line();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS stocktaking_lines_protect ON inventory.stocktaking_lines;
                DROP TRIGGER IF EXISTS stocktakings_protect ON inventory.stocktakings;
                DROP FUNCTION IF EXISTS inventory.protect_stocktaking_line();
                DROP FUNCTION IF EXISTS inventory.protect_stocktaking();
                """);

            migrationBuilder.DropTable(
                name: "stocktaking_lines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "stocktakings",
                schema: "inventory");

            migrationBuilder.DropSequence(
                name: "stocktaking_numbers",
                schema: "inventory");
        }
    }
}
