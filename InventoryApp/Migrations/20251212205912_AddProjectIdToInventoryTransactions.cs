using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectIdToInventoryTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "InventoryTransactions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ProjectId",
                table: "InventoryTransactions",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Projects_ProjectId",
                table: "InventoryTransactions",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Projects_ProjectId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_ProjectId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "InventoryTransactions");
        }
    }
}
