using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace danserdan.Migrations
{
    /// <inheritdoc />
    public partial class AddSectorColumnToStocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "stock_symbol",
                table: "transactions");

            migrationBuilder.RenameColumn(
                name: "transaction_date",
                table: "transactions",
                newName: "transaction_time");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "transactions",
                newName: "price");

            migrationBuilder.AlterColumn<string>(
                name: "transaction_type",
                table: "transactions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "stock_id",
                table: "transactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "Stocks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "sector",
                table: "Stocks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "stock_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "sector",
                table: "Stocks");

            migrationBuilder.RenameColumn(
                name: "transaction_time",
                table: "transactions",
                newName: "transaction_date");

            migrationBuilder.RenameColumn(
                name: "price",
                table: "transactions",
                newName: "amount");

            migrationBuilder.AlterColumn<string>(
                name: "transaction_type",
                table: "transactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stock_symbol",
                table: "transactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
