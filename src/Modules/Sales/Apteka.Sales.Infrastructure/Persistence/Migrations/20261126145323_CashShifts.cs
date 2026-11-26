using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Sales.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CashShifts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "cash_shift_numbers",
                schema: "sales");

            migrationBuilder.CreateTable(
                name: "cash_shifts",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('sales.cash_shift_numbers')"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opened_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    opening_cash = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expected_cash = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    counted_cash = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_shifts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cash_movements",
                schema: "sales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_movements", x => x.id);
                    table.CheckConstraint("ck_cash_movements_amount", "amount <> 0");
                    table.ForeignKey(
                        name: "fk_cash_movements_cash_shifts_shift_id",
                        column: x => x.shift_id,
                        principalSchema: "sales",
                        principalTable: "cash_shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cash_movements_shift_id",
                schema: "sales",
                table: "cash_movements",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_cash_shifts_number",
                schema: "sales",
                table: "cash_shifts",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_cash_shifts_one_open_per_user",
                schema: "sales",
                table: "cash_shifts",
                column: "user_id",
                unique: true,
                filter: "closed_at IS NULL");

            // A shift is never deleted and does not change once closed; cash movements only grow.
            migrationBuilder.Sql("""
                CREATE FUNCTION sales.protect_cash_shift() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION 'sales.cash_shifts rows are never deleted';
                    END IF;
                    IF OLD.closed_at IS NOT NULL THEN
                        RAISE EXCEPTION 'cash shift % is closed and cannot change', OLD.number;
                    END IF;
                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER cash_shifts_protect BEFORE UPDATE OR DELETE ON sales.cash_shifts
                    FOR EACH ROW EXECUTE FUNCTION sales.protect_cash_shift();
                CREATE TRIGGER cash_movements_append_only BEFORE UPDATE OR DELETE ON sales.cash_movements
                    FOR EACH ROW EXECUTE FUNCTION sales.reject_document_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS cash_movements_append_only ON sales.cash_movements;
                DROP TRIGGER IF EXISTS cash_shifts_protect ON sales.cash_shifts;
                DROP FUNCTION IF EXISTS sales.protect_cash_shift();
                """);

            migrationBuilder.DropTable(
                name: "cash_movements",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "cash_shifts",
                schema: "sales");

            migrationBuilder.DropSequence(
                name: "cash_shift_numbers",
                schema: "sales");
        }
    }
}
