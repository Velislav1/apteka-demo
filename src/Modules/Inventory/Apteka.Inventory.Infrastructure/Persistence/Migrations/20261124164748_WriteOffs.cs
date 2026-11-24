using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WriteOffs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "write_off_numbers",
                schema: "inventory");

            migrationBuilder.CreateTable(
                name: "write_offs",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('inventory.write_off_numbers')"),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_write_offs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "write_off_lines",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    write_off_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    unit_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    unit_factor = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    purchase_price = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_write_off_lines", x => x.id);
                    table.CheckConstraint("ck_write_off_lines_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_write_off_lines_batches_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "inventory",
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_write_off_lines_write_offs_write_off_id",
                        column: x => x.write_off_id,
                        principalSchema: "inventory",
                        principalTable: "write_offs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_write_off_lines_batch_id",
                schema: "inventory",
                table: "write_off_lines",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_write_off_lines_product_id",
                schema: "inventory",
                table: "write_off_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_write_off_lines_write_off_id",
                schema: "inventory",
                table: "write_off_lines",
                column: "write_off_id");

            migrationBuilder.CreateIndex(
                name: "ix_write_offs_created_at",
                schema: "inventory",
                table: "write_offs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_write_offs_number",
                schema: "inventory",
                table: "write_offs",
                column: "number",
                unique: true);

            // Rule 6: a completed write-off is never changed or deleted.
            migrationBuilder.Sql("""
                CREATE FUNCTION inventory.reject_document_change() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION '%.% is append-only (% is not allowed)', TG_TABLE_SCHEMA, TG_TABLE_NAME, TG_OP;
                END;
                $$;

                CREATE TRIGGER write_offs_append_only BEFORE UPDATE OR DELETE ON inventory.write_offs
                    FOR EACH ROW EXECUTE FUNCTION inventory.reject_document_change();
                CREATE TRIGGER write_off_lines_append_only BEFORE UPDATE OR DELETE ON inventory.write_off_lines
                    FOR EACH ROW EXECUTE FUNCTION inventory.reject_document_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS write_off_lines_append_only ON inventory.write_off_lines;
                DROP TRIGGER IF EXISTS write_offs_append_only ON inventory.write_offs;
                DROP FUNCTION IF EXISTS inventory.reject_document_change();
                """);

            migrationBuilder.DropTable(
                name: "write_off_lines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "write_offs",
                schema: "inventory");

            migrationBuilder.DropSequence(
                name: "write_off_numbers",
                schema: "inventory");
        }
    }
}
