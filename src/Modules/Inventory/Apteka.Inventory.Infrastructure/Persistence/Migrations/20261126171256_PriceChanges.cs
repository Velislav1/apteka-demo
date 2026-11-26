using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PriceChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "price_change_numbers",
                schema: "inventory");

            migrationBuilder.CreateTable(
                name: "price_changes",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('inventory.price_change_numbers')"),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_changes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "price_change_lines",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_change_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    priced_unit_factor = table.Column<int>(type: "integer", nullable: false),
                    old_price = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    new_price = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_change_lines", x => x.id);
                    table.CheckConstraint("ck_price_change_lines_new_price", "new_price > 0");
                    table.ForeignKey(
                        name: "fk_price_change_lines_batches_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "inventory",
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_price_change_lines_price_changes_price_change_id",
                        column: x => x.price_change_id,
                        principalSchema: "inventory",
                        principalTable: "price_changes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_price_change_lines_batch_id",
                schema: "inventory",
                table: "price_change_lines",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_price_change_lines_price_change_id",
                schema: "inventory",
                table: "price_change_lines",
                column: "price_change_id");

            migrationBuilder.CreateIndex(
                name: "ix_price_change_lines_product_id",
                schema: "inventory",
                table: "price_change_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_price_changes_number",
                schema: "inventory",
                table: "price_changes",
                column: "number",
                unique: true);

            // A price change record never changes (rule 6).
            migrationBuilder.Sql("""
                CREATE TRIGGER price_changes_append_only BEFORE UPDATE OR DELETE ON inventory.price_changes
                    FOR EACH ROW EXECUTE FUNCTION inventory.reject_document_change();
                CREATE TRIGGER price_change_lines_append_only BEFORE UPDATE OR DELETE ON inventory.price_change_lines
                    FOR EACH ROW EXECUTE FUNCTION inventory.reject_document_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS price_change_lines_append_only ON inventory.price_change_lines;
                DROP TRIGGER IF EXISTS price_changes_append_only ON inventory.price_changes;
                """);

            migrationBuilder.DropTable(
                name: "price_change_lines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "price_changes",
                schema: "inventory");

            migrationBuilder.DropSequence(
                name: "price_change_numbers",
                schema: "inventory");
        }
    }
}
