using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "Components",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Rack = table.Column<string>(type: "TEXT", nullable: false),
                    Drawer = table.Column<string>(type: "TEXT", nullable: true),
                    Box = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Components_LocationId",
                table: "Components",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Components_Locations_LocationId",
                table: "Components",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Components_Locations_LocationId",
                table: "Components");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Components_LocationId",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Components");
        }
    }
}
