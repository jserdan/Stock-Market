using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace danserdan.Migrations
{
    /// <inheritdoc />
    public partial class UseFirstNameLastNameDirectly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "last_name",
                table: "users",
                newName: "lastName");

            migrationBuilder.RenameColumn(
                name: "first_name",
                table: "users",
                newName: "firstName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "lastName",
                table: "users",
                newName: "last_name");

            migrationBuilder.RenameColumn(
                name: "firstName",
                table: "users",
                newName: "first_name");
        }
    }
}
