using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Inventory.Migrations
{
    /// <inheritdoc />
    public partial class FreshStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockOut_Date",
                table: "StockOuts");

            migrationBuilder.DropIndex(
                name: "IX_StockIn_Date",
                table: "StockIns");

            migrationBuilder.DropIndex(
                name: "IX_Product_Name",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Product_Quantity",
                table: "Products");

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "Password", "Role", "UserName", "isActive", "isDelete" },
                values: new object[,]
                {
                    { 1, "admin@inventory.com", "admin123", "Admin", "admin", true, false },
                    { 2, "manager@inventory.com", "manager123", "Manager", "manager", true, false },
                    { 3, "staff@inventory.com", "staff123", "Staff", "staff", true, false }
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockOut_Date",
                table: "StockOuts",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_StockIn_Date",
                table: "StockIns",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Name",
                table: "Products",
                column: "ProductName");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Quantity",
                table: "Products",
                column: "Quantity");
        }
    }
}
