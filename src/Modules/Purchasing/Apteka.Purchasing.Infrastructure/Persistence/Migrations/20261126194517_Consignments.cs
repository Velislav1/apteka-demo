using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Apteka.Purchasing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Consignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_consignment",
                schema: "purchasing",
                table: "deliveries",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_consignment",
                schema: "purchasing",
                table: "deliveries");
        }
    }
}
