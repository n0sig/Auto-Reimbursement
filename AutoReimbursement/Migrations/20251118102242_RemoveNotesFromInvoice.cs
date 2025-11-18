using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoReimbursement.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNotesFromInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_AspNetUsers_PayerId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "PayerId",
                table: "Invoices",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_AspNetUsers_PayerId",
                table: "Invoices",
                column: "PayerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_AspNetUsers_PayerId",
                table: "Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "PayerId",
                table: "Invoices",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Invoices",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_AspNetUsers_PayerId",
                table: "Invoices",
                column: "PayerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
