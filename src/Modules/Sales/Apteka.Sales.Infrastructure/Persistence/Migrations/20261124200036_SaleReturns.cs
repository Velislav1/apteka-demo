using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Sales.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SaleReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "sale_return_numbers",
                schema: "sales");

            migrationBuilder.AddColumn<int>(
                name: "position",
                schema: "sales",
                table: "sale_line_batches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "sale_returns",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('sales.sale_return_numbers')"),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    vat_total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_returns", x => x.id);
                    table.ForeignKey(
                        name: "fk_sale_returns_sales_sale_id",
                        column: x => x.sale_id,
                        principalSchema: "sales",
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sale_return_lines",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_return_lines", x => x.id);
                    table.CheckConstraint("ck_sale_return_lines_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_sale_return_lines_sale_lines_sale_line_id",
                        column: x => x.sale_line_id,
                        principalSchema: "sales",
                        principalTable: "sale_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sale_return_lines_sale_returns_return_id",
                        column: x => x.return_id,
                        principalSchema: "sales",
                        principalTable: "sale_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sale_return_line_batches",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sale_return_line_batches", x => x.id);
                    table.CheckConstraint("ck_sale_return_line_batches_quantity", "base_quantity > 0");
                    table.ForeignKey(
                        name: "fk_sale_return_line_batches_sale_return_lines_return_line_id",
                        column: x => x.return_line_id,
                        principalSchema: "sales",
                        principalTable: "sale_return_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sale_return_line_batches_batch_id",
                schema: "sales",
                table: "sale_return_line_batches",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_return_line_batches_return_line_id",
                schema: "sales",
                table: "sale_return_line_batches",
                column: "return_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_return_lines_return_id",
                schema: "sales",
                table: "sale_return_lines",
                column: "return_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_return_lines_sale_line_id",
                schema: "sales",
                table: "sale_return_lines",
                column: "sale_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_sale_returns_created_at",
                schema: "sales",
                table: "sale_returns",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_sale_returns_number",
                schema: "sales",
                table: "sale_returns",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sale_returns_sale_id",
                schema: "sales",
                table: "sale_returns",
                column: "sale_id");

            // Rule 6: a correction of a sale is never changed or deleted.
            migrationBuilder.Sql("""
                CREATE FUNCTION sales.reject_document_change() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION '%.% is append-only (% is not allowed)', TG_TABLE_SCHEMA, TG_TABLE_NAME, TG_OP;
                END;
                $$;

                CREATE TRIGGER sale_returns_append_only BEFORE UPDATE OR DELETE ON sales.sale_returns
                    FOR EACH ROW EXECUTE FUNCTION sales.reject_document_change();
                CREATE TRIGGER sale_return_lines_append_only BEFORE UPDATE OR DELETE ON sales.sale_return_lines
                    FOR EACH ROW EXECUTE FUNCTION sales.reject_document_change();
                CREATE TRIGGER sale_return_line_batches_append_only BEFORE UPDATE OR DELETE ON sales.sale_return_line_batches
                    FOR EACH ROW EXECUTE FUNCTION sales.reject_document_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS sale_return_line_batches_append_only ON sales.sale_return_line_batches;
                DROP TRIGGER IF EXISTS sale_return_lines_append_only ON sales.sale_return_lines;
                DROP TRIGGER IF EXISTS sale_returns_append_only ON sales.sale_returns;
                DROP FUNCTION IF EXISTS sales.reject_document_change();
                """);

            migrationBuilder.DropTable(
                name: "sale_return_line_batches",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sale_return_lines",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "sale_returns",
                schema: "sales");

            migrationBuilder.DropColumn(
                name: "position",
                schema: "sales",
                table: "sale_line_batches");

            migrationBuilder.DropSequence(
                name: "sale_return_numbers",
                schema: "sales");
        }
    }
}
