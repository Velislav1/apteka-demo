using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WriteOffKinds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "kind",
                schema: "inventory",
                table: "write_offs",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "WriteOff");

            migrationBuilder.AddColumn<string>(
                name: "recipient",
                schema: "inventory",
                table: "write_offs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "kind",
                schema: "inventory",
                table: "write_offs");

            migrationBuilder.DropColumn(
                name: "recipient",
                schema: "inventory",
                table: "write_offs");
        }
    }
}
