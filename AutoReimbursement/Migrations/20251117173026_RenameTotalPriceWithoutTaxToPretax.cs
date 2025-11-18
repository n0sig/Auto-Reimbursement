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
                
            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "InvoiceItems",
                newName: "Pretax_Old");
                
            migrationBuilder.DropColumn(
                name: "Pretax_Old",
                table: "InvoiceItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Pretax",
                table: "InvoiceItems",
                newName: "TotalPriceWithoutTax");
                
            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "InvoiceItems",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
