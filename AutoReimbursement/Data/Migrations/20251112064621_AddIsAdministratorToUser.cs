using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoReimbursement.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAdministratorToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdministrator",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAdministrator",
                table: "AspNetUsers");
        }
    }
}
