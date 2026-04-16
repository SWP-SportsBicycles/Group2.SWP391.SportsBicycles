using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Group2.SWP391.SportsBicycles.DAL.Migrations
{
    /// <inheritdoc />
    public partial class ADdnewAccountADmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AvtUrl", "CreatedAt", "Email", "FirebaseUID", "FullName", "IsDeleted", "Password", "PhoneNumber", "Role", "Status", "UpdatedAt", "WalletBalance" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), null, new DateTime(2026, 4, 16, 0, 0, 0, 0, DateTimeKind.Utc), "admin@sportsbicycles.com", null, "System Admin", false, "100000.c3BvcnRzYmljeWNsZXNfYWRtbg==.eoKYA/2sVdzZZ1GWgZxQB/tfujoNPsiJ1rTC8zRmR+0=", "0909000000", 1, 1, new DateTime(2026, 4, 16, 0, 0, 0, 0, DateTimeKind.Utc), 0m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));
        }
    }
}
