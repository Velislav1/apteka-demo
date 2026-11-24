using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Audit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.CreateTable(
                name: "audit_log",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    entity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_at",
                schema: "audit",
                table: "audit_log",
                column: "at");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_entity_entity_id",
                schema: "audit",
                table: "audit_log",
                columns: new[] { "entity", "entity_id" });

            // The audit log is append-only: nobody (not even the application) may change or remove a row.
            migrationBuilder.Sql("""
                CREATE FUNCTION audit.reject_audit_log_change() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'audit.audit_log is append-only (% is not allowed)', TG_OP;
                END;
                $$;

                CREATE TRIGGER audit_log_append_only
                BEFORE UPDATE OR DELETE ON audit.audit_log
                FOR EACH ROW EXECUTE FUNCTION audit.reject_audit_log_change();

                CREATE TRIGGER audit_log_no_truncate
                BEFORE TRUNCATE ON audit.audit_log
                FOR EACH STATEMENT EXECUTE FUNCTION audit.reject_audit_log_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS audit_log_no_truncate ON audit.audit_log;
                DROP TRIGGER IF EXISTS audit_log_append_only ON audit.audit_log;
                DROP FUNCTION IF EXISTS audit.reject_audit_log_change();
                """);

            migrationBuilder.DropTable(
                name: "audit_log",
                schema: "audit");
        }
    }
}
