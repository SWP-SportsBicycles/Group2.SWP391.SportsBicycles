using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Group2.SWP391.SportsBicycles.DAL.Migrations
{
    /// <inheritdoc />
    public partial class updateprofileSeller : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccountName",
                table: "SellerShippingProfile",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "SellerShippingProfile",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "SellerShippingProfile",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankAccountName",
                table: "SellerShippingProfile");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "SellerShippingProfile");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "SellerShippingProfile");
        }
    }
}
