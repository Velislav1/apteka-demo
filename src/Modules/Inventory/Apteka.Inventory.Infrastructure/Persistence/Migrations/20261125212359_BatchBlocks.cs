using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BatchBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "batch_blocks",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_batch_blocks", x => x.id);
                    table.ForeignKey(
                        name: "fk_batch_blocks_batches_batch_id",
                        column: x => x.batch_id,
                        principalSchema: "inventory",
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_batch_blocks_batch_id_at",
                schema: "inventory",
                table: "batch_blocks",
                columns: new[] { "batch_id", "at" });

            // The blocking history only grows (rule 6).
            migrationBuilder.Sql("""
                CREATE TRIGGER batch_blocks_append_only BEFORE UPDATE OR DELETE ON inventory.batch_blocks
                    FOR EACH ROW EXECUTE FUNCTION inventory.reject_document_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS batch_blocks_append_only ON inventory.batch_blocks;");

            migrationBuilder.DropTable(
                name: "batch_blocks",
                schema: "inventory");
        }
    }
}
