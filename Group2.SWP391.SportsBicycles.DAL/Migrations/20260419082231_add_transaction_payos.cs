using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Group2.SWP391.SportsBicycles.DAL.Migrations
{
    /// <inheritdoc />
    public partial class add_transaction_payos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_OrderCode",
                table: "Transactions");

            migrationBuilder.AlterColumn<string>(
                name: "OrderCode",
                table: "Transactions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentLink",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderOrderCode",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OrderCode",
                table: "Transactions",
                column: "OrderCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_OrderCode",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PaymentLink",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ProviderOrderCode",
                table: "Transactions");

            migrationBuilder.AlterColumn<string>(
                name: "OrderCode",
                table: "Transactions",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OrderCode",
                table: "Transactions",
                column: "OrderCode",
                unique: true,
                filter: "[OrderCode] IS NOT NULL");
        }
    }
}
