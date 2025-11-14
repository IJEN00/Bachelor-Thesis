using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSelectedToSupplierOffer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSelected",
                table: "SupplierOffers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSelected",
                table: "SupplierOffers");
        }
    }
}
