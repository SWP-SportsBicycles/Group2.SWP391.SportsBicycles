    using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Group2.SWP391.SportsBicycles.DAL.Migrations
{
    /// <inheritdoc />
    public partial class updatedbnew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ToProvinceId",
                table: "Orders",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ToProvinceId",
                table: "Orders");
        }
    }
}
