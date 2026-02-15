using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nexa.Api.Migrations
{
    /// <inheritdoc />
    public partial class prices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "PriceMin",
                table: "Listings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "PriceMax",
                table: "Listings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "Interests", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 15, 9, 26, 59, 937, DateTimeKind.Utc).AddTicks(3521), new List<string> { "electronics", "tech" }, new DateTime(2026, 2, 15, 9, 26, 59, 937, DateTimeKind.Utc).AddTicks(3522) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "Interests", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 15, 9, 26, 59, 937, DateTimeKind.Utc).AddTicks(3526), new List<string> { "fashion", "home" }, new DateTime(2026, 2, 15, 9, 26, 59, 937, DateTimeKind.Utc).AddTicks(3527) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "PriceMin",
                table: "Listings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PriceMax",
                table: "Listings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "Interests", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 15, 9, 14, 39, 108, DateTimeKind.Utc).AddTicks(6428), new List<string> { "electronics", "tech" }, new DateTime(2026, 2, 15, 9, 14, 39, 108, DateTimeKind.Utc).AddTicks(6429) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "Interests", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 2, 15, 9, 14, 39, 108, DateTimeKind.Utc).AddTicks(6433), new List<string> { "fashion", "home" }, new DateTime(2026, 2, 15, 9, 14, 39, 108, DateTimeKind.Utc).AddTicks(6434) });
        }
    }
}
