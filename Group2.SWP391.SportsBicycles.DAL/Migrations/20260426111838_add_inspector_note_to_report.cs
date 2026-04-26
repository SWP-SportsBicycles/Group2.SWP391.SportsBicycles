using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Group2.SWP391.SportsBicycles.DAL.Migrations
{
    /// <inheritdoc />
    public partial class add_inspector_note_to_report : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InspectorNote",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InspectorNote",
                table: "Reports");
        }
    }
}
