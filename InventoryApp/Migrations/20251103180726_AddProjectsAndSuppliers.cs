using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryApp.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectsAndSuppliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "TEXT", nullable: true),
                    HasApi = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApiBaseUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ApiKey = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomName = table.Column<string>(type: "TEXT", nullable: true),
                    QuantityRequired = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityFromStock = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityToBuy = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectItems_Components_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Components",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProjectItems_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    SupplierId = table.Column<int>(type: "INTEGER", nullable: false),
                    SupplierPartNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false, defaultValue: "EUR"),
                    InStock = table.Column<bool>(type: "INTEGER", nullable: false),
                    MinOrderQty = table.Column<int>(type: "INTEGER", nullable: false),
                    LeadTimeDays = table.Column<int>(type: "INTEGER", nullable: true),
                    ProductUrl = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierOffers_ProjectItems_ProjectItemId",
                        column: x => x.ProjectItemId,
                        principalTable: "ProjectItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierOffers_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectItems_ComponentId",
                table: "ProjectItems",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectItems_ProjectId",
                table: "ProjectItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOffers_ProjectItemId",
                table: "SupplierOffers",
                column: "ProjectItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierOffers_SupplierId",
                table: "SupplierOffers",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierOffers");

            migrationBuilder.DropTable(
                name: "ProjectItems");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
