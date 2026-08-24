using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishingLog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFishingTripOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FishingTrips_LastModified",
                table: "FishingTrips");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "FishingTrips",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_FishingTrips_LastModified_UserId",
                table: "FishingTrips",
                columns: new[] { "LastModified", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FishingTrips_LastModified_UserId",
                table: "FishingTrips");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "FishingTrips");

            migrationBuilder.CreateIndex(
                name: "IX_FishingTrips_LastModified",
                table: "FishingTrips",
                column: "LastModified");
        }
    }
}
