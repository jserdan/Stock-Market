using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace danserdan.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenPriceFieldsToStocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    stockid = table.Column<int>(name: "stock_id", type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    symbol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    companyname = table.Column<string>(name: "company_name", type: "nvarchar(max)", nullable: false),
                    marketprice = table.Column<decimal>(name: "market_price", type: "decimal(18,2)", nullable: false),
                    lastupdated = table.Column<DateTime>(name: "last_updated", type: "datetime2", nullable: false),
                    openprice = table.Column<decimal>(name: "open_price", type: "decimal(18,2)", nullable: true),
                    openpricetime = table.Column<DateTime>(name: "open_price_time", type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.stockid);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Stocks");

            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
