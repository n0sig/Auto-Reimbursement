using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoReimbursement.Migrations
{
    /// <inheritdoc />
    public partial class RenameTotalPriceWithoutTaxToPretax : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalPriceWithoutTax",
                table: "InvoiceItems",
                newName: "Pretax");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Pretax",
                table: "InvoiceItems",
                newName: "TotalPriceWithoutTax");
        }
    }
}
