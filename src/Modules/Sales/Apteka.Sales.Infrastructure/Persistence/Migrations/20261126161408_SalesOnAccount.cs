using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Sales.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SalesOnAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "payment_term_days",
                schema: "sales",
                table: "sales",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "customer_payments",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    paid_on = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    channel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_payments", x => x.id);
                    table.CheckConstraint("ck_customer_payments_amount", "amount > 0");
                    table.ForeignKey(
                        name: "fk_customer_payments_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "sales",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_payment_allocations",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_payment_allocations", x => x.id);
                    table.CheckConstraint("ck_customer_payment_allocations_amount", "amount > 0");
                    table.ForeignKey(
                        name: "fk_customer_payment_allocations_customer_payments_payment_id",
                        column: x => x.payment_id,
                        principalSchema: "sales",
                        principalTable: "customer_payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_customer_payment_allocations_sales_sale_id",
                        column: x => x.sale_id,
                        principalSchema: "sales",
                        principalTable: "sales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_payment_allocations_payment_id",
                schema: "sales",
                table: "customer_payment_allocations",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_payment_allocations_sale_id",
                schema: "sales",
                table: "customer_payment_allocations",
                column: "sale_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_payments_customer_id",
                schema: "sales",
                table: "customer_payments",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_payments_paid_on",
                schema: "sales",
                table: "customer_payments",
                column: "paid_on");

            // A customer payment never changes.
            migrationBuilder.Sql("""
                CREATE TRIGGER customer_payments_append_only BEFORE UPDATE OR DELETE ON sales.customer_payments
                    FOR EACH ROW EXECUTE FUNCTION sales.reject_document_change();
                CREATE TRIGGER customer_payment_allocations_append_only BEFORE UPDATE OR DELETE ON sales.customer_payment_allocations
                    FOR EACH ROW EXECUTE FUNCTION sales.reject_document_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS customer_payment_allocations_append_only ON sales.customer_payment_allocations;
                DROP TRIGGER IF EXISTS customer_payments_append_only ON sales.customer_payments;
                """);

            migrationBuilder.DropTable(
                name: "customer_payment_allocations",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "customer_payments",
                schema: "sales");

            migrationBuilder.DropColumn(
                name: "payment_term_days",
                schema: "sales",
                table: "sales");
        }
    }
}
