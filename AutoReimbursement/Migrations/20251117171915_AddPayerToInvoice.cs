using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoReimbursement.Migrations
{
    /// <inheritdoc />
    public partial class AddPayerToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayerId",
                table: "Invoices",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PayerId",
                table: "Invoices",
                column: "PayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_AspNetUsers_PayerId",
                table: "Invoices",
                column: "PayerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_AspNetUsers_PayerId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_PayerId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PayerId",
                table: "Invoices");
        }
    }
}
