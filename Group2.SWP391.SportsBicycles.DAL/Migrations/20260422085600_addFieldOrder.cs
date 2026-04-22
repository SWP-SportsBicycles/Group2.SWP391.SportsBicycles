using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Group2.SWP391.SportsBicycles.DAL.Migrations
{
    /// <inheritdoc />
    public partial class addFieldOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidOutAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PayoutEligibleAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SellerNotifiedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaidOutAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PayoutEligibleAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SellerNotifiedAt",
                table: "Orders");
        }
    }
}
