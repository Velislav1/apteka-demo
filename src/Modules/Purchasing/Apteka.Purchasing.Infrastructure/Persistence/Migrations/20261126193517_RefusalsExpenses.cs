using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Purchasing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefusalsExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "expenses",
                schema: "purchasing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    document_no = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expenses", x => x.id);
                    table.CheckConstraint("ck_expenses_amount", "amount <> 0");
                });

            migrationBuilder.CreateTable(
                name: "refusals",
                schema: "purchasing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refusals", x => x.id);
                    table.CheckConstraint("ck_refusals_quantity", "quantity > 0");
                });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_date",
                schema: "purchasing",
                table: "expenses",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "ix_refusals_created_at",
                schema: "purchasing",
                table: "refusals",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_refusals_product_id",
                schema: "purchasing",
                table: "refusals",
                column: "product_id");

            // Refusals and expenses are records: never changed or deleted (rule 6).
            migrationBuilder.Sql("""
                CREATE TRIGGER refusals_append_only BEFORE UPDATE OR DELETE ON purchasing.refusals
                    FOR EACH ROW EXECUTE FUNCTION purchasing.reject_document_change();
                CREATE TRIGGER expenses_append_only BEFORE UPDATE OR DELETE ON purchasing.expenses
                    FOR EACH ROW EXECUTE FUNCTION purchasing.reject_document_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS expenses_append_only ON purchasing.expenses;
                DROP TRIGGER IF EXISTS refusals_append_only ON purchasing.refusals;
                """);

            migrationBuilder.DropTable(
                name: "expenses",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "refusals",
                schema: "purchasing");
        }
    }
}
