using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoReimbursement.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserIdFromReimbursementPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReimbursementPlans_AspNetUsers_UserId",
                table: "ReimbursementPlans");

            migrationBuilder.DropIndex(
                name: "IX_ReimbursementPlans_UserId",
                table: "ReimbursementPlans");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ReimbursementPlans");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ReimbursementPlans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ReimbursementPlans_UserId",
                table: "ReimbursementPlans",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReimbursementPlans_AspNetUsers_UserId",
                table: "ReimbursementPlans",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
