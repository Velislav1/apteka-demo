using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Purchasing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeliveryCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "delivery_correction_numbers",
                schema: "purchasing");

            migrationBuilder.CreateTable(
                name: "delivery_corrections",
                schema: "purchasing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('purchasing.delivery_correction_numbers')"),
                    delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    note_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    note_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_corrections", x => x.id);
                    table.ForeignKey(
                        name: "fk_delivery_corrections_deliveries_delivery_id",
                        column: x => x.delivery_id,
                        principalSchema: "purchasing",
                        principalTable: "deliveries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "delivery_correction_lines",
                schema: "purchasing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    correction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    delivery_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_line_position = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_factor = table.Column<int>(type: "integer", nullable: false),
                    quantity_delta = table.Column<int>(type: "integer", nullable: false),
                    unit_purchase_price = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_correction_lines", x => x.id);
                    table.CheckConstraint("ck_delivery_correction_lines_delta", "quantity_delta <> 0");
                    table.ForeignKey(
                        name: "fk_delivery_correction_lines_delivery_corrections_correction_id",
                        column: x => x.correction_id,
                        principalSchema: "purchasing",
                        principalTable: "delivery_corrections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_delivery_correction_lines_delivery_lines_delivery_line_id",
                        column: x => x.delivery_line_id,
                        principalSchema: "purchasing",
                        principalTable: "delivery_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_delivery_correction_lines_correction_id",
                schema: "purchasing",
                table: "delivery_correction_lines",
                column: "correction_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_correction_lines_delivery_line_id",
                schema: "purchasing",
                table: "delivery_correction_lines",
                column: "delivery_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_corrections_created_at",
                schema: "purchasing",
                table: "delivery_corrections",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_corrections_delivery_id",
                schema: "purchasing",
                table: "delivery_corrections",
                column: "delivery_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_corrections_number",
                schema: "purchasing",
                table: "delivery_corrections",
                column: "number",
                unique: true);

            // Rule 6: a saved correction is never changed or deleted.
            migrationBuilder.Sql("""
                CREATE FUNCTION purchasing.reject_document_change() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION '%.% is append-only (% is not allowed)', TG_TABLE_SCHEMA, TG_TABLE_NAME, TG_OP;
                END;
                $$;

                CREATE TRIGGER delivery_corrections_append_only BEFORE UPDATE OR DELETE ON purchasing.delivery_corrections
                    FOR EACH ROW EXECUTE FUNCTION purchasing.reject_document_change();
                CREATE TRIGGER delivery_correction_lines_append_only BEFORE UPDATE OR DELETE ON purchasing.delivery_correction_lines
                    FOR EACH ROW EXECUTE FUNCTION purchasing.reject_document_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS delivery_correction_lines_append_only ON purchasing.delivery_correction_lines;
                DROP TRIGGER IF EXISTS delivery_corrections_append_only ON purchasing.delivery_corrections;
                DROP FUNCTION IF EXISTS purchasing.reject_document_change();
                """);

            migrationBuilder.DropTable(
                name: "delivery_correction_lines",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "delivery_corrections",
                schema: "purchasing");

            migrationBuilder.DropSequence(
                name: "delivery_correction_numbers",
                schema: "purchasing");
        }
    }
}
