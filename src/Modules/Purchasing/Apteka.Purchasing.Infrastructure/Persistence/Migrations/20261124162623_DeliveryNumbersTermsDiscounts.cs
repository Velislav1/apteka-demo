using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Purchasing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DeliveryNumbersTermsDiscounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "delivery_numbers",
                schema: "purchasing");

            migrationBuilder.AddColumn<decimal>(
                name: "supplier_discount_percent",
                schema: "purchasing",
                table: "delivery_lines",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "number",
                schema: "purchasing",
                table: "deliveries",
                type: "bigint",
                nullable: false,
                defaultValueSql: "nextval('purchasing.delivery_numbers')");

            migrationBuilder.AddColumn<int>(
                name: "payment_term_days",
                schema: "purchasing",
                table: "deliveries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Existing deliveries take the payment term of their supplier.
            migrationBuilder.Sql(
                "UPDATE purchasing.deliveries d SET payment_term_days = s.payment_term_days FROM purchasing.suppliers s WHERE s.id = d.supplier_id;");

            migrationBuilder.CreateIndex(
                name: "ix_deliveries_number",
                schema: "purchasing",
                table: "deliveries",
                column: "number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_deliveries_number",
                schema: "purchasing",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "supplier_discount_percent",
                schema: "purchasing",
                table: "delivery_lines");

            migrationBuilder.DropColumn(
                name: "number",
                schema: "purchasing",
                table: "deliveries");

            migrationBuilder.DropColumn(
                name: "payment_term_days",
                schema: "purchasing",
                table: "deliveries");

            migrationBuilder.DropSequence(
                name: "delivery_numbers",
                schema: "purchasing");
        }
    }
}
