using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Sales.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Customers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "customer_codes",
                schema: "sales");

            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                schema: "sales",
                table: "sales",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('sales.customer_codes')"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    egn = table.Column<string>(type: "character(10)", fixedLength: true, maxLength: 10, nullable: true),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sales_customer_id",
                schema: "sales",
                table: "sales",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_code",
                schema: "sales",
                table: "customers",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customers_egn",
                schema: "sales",
                table: "customers",
                column: "egn",
                unique: true,
                filter: "egn IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_customers_name",
                schema: "sales",
                table: "customers",
                column: "name");

            migrationBuilder.AddForeignKey(
                name: "fk_sales_customers_customer_id",
                schema: "sales",
                table: "sales",
                column: "customer_id",
                principalSchema: "sales",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            // Rule 6: a customer is deactivated, never deleted.
            migrationBuilder.Sql("""
                CREATE FUNCTION sales.reject_customer_delete() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'sales.customers rows are never deleted; deactivate the customer instead';
                END;
                $$;

                CREATE TRIGGER customers_no_delete BEFORE DELETE ON sales.customers
                    FOR EACH ROW EXECUTE FUNCTION sales.reject_customer_delete();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS customers_no_delete ON sales.customers;
                DROP FUNCTION IF EXISTS sales.reject_customer_delete();
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_sales_customers_customer_id",
                schema: "sales",
                table: "sales");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "sales");

            migrationBuilder.DropIndex(
                name: "ix_sales_customer_id",
                schema: "sales",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "customer_id",
                schema: "sales",
                table: "sales");

            migrationBuilder.DropSequence(
                name: "customer_codes",
                schema: "sales");
        }
    }
}
