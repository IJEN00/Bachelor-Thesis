using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Migrations
{
    /// <inheritdoc />
    public partial class SetProjectIdNullOnDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Projects_ProjectId",
                table: "InventoryTransactions");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Projects_ProjectId",
                table: "InventoryTransactions",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Projects_ProjectId",
                table: "InventoryTransactions");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_Projects_ProjectId",
                table: "InventoryTransactions",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");
        }
    }
}
