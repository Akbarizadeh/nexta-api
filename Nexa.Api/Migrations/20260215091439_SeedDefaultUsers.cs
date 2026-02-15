using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Nexa.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AvatarUrl", "CreatedAt", "DisplayName", "Email", "Interests", "Latitude", "Longitude", "Role", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), null, new DateTime(2026, 2, 15, 9, 14, 39, 108, DateTimeKind.Utc).AddTicks(6428), "Test Seller 1", "seller1@nexa.com", new List<string> { "electronics", "tech" }, null, null, 1, new DateTime(2026, 2, 15, 9, 14, 39, 108, DateTimeKind.Utc).AddTicks(6429) },
                    { new Guid("22222222-2222-2222-2222-222222222222"), null, new DateTime(2026, 2, 15, 9, 14, 39, 108, DateTimeKind.Utc).AddTicks(6433), "Test Seller 2", "seller2@nexa.com", new List<string> { "fashion", "home" }, null, null, 1, new DateTime(2026, 2, 15, 9, 14, 39, 108, DateTimeKind.Utc).AddTicks(6434) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));
        }
    }
}
