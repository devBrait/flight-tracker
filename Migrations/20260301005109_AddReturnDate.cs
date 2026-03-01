using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FlightPrices_Origin_Destination_DepartureDate_CheckedAt",
                table: "FlightPrices");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnDate",
                table: "FlightPrices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlightPrices_Origin_Destination_DepartureDate_ReturnDate_CheckedAt",
                table: "FlightPrices",
                columns: new[] { "Origin", "Destination", "DepartureDate", "ReturnDate", "CheckedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FlightPrices_Origin_Destination_DepartureDate_ReturnDate_CheckedAt",
                table: "FlightPrices");

            migrationBuilder.DropColumn(
                name: "ReturnDate",
                table: "FlightPrices");

            migrationBuilder.CreateIndex(
                name: "IX_FlightPrices_Origin_Destination_DepartureDate_CheckedAt",
                table: "FlightPrices",
                columns: new[] { "Origin", "Destination", "DepartureDate", "CheckedAt" });
        }
    }
}
