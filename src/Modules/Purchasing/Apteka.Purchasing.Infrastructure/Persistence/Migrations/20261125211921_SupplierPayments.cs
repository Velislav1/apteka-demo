using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Purchasing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SupplierPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_payments",
                schema: "purchasing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    paid_on = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    channel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_payments", x => x.id);
                    table.CheckConstraint("ck_supplier_payments_amount", "amount > 0");
                    table.ForeignKey(
                        name: "fk_supplier_payments_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "purchasing",
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_allocations",
                schema: "purchasing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_allocations", x => x.id);
                    table.CheckConstraint("ck_payment_allocations_amount", "amount > 0");
                    table.ForeignKey(
                        name: "fk_payment_allocations_deliveries_delivery_id",
                        column: x => x.delivery_id,
                        principalSchema: "purchasing",
                        principalTable: "deliveries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_allocations_supplier_payments_payment_id",
                        column: x => x.payment_id,
                        principalSchema: "purchasing",
                        principalTable: "supplier_payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocations_delivery_id",
                schema: "purchasing",
                table: "payment_allocations",
                column: "delivery_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocations_payment_id",
                schema: "purchasing",
                table: "payment_allocations",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payments_paid_on",
                schema: "purchasing",
                table: "supplier_payments",
                column: "paid_on");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_payments_supplier_id",
                schema: "purchasing",
                table: "supplier_payments",
                column: "supplier_id");

            // A payment is never changed or deleted; a mistake is fixed with a new document.
            migrationBuilder.Sql("""
                CREATE TRIGGER supplier_payments_append_only BEFORE UPDATE OR DELETE ON purchasing.supplier_payments
                    FOR EACH ROW EXECUTE FUNCTION purchasing.reject_document_change();
                CREATE TRIGGER payment_allocations_append_only BEFORE UPDATE OR DELETE ON purchasing.payment_allocations
                    FOR EACH ROW EXECUTE FUNCTION purchasing.reject_document_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS payment_allocations_append_only ON purchasing.payment_allocations;
                DROP TRIGGER IF EXISTS supplier_payments_append_only ON purchasing.supplier_payments;
                """);

            migrationBuilder.DropTable(
                name: "payment_allocations",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "supplier_payments",
                schema: "purchasing");
        }
    }
}
