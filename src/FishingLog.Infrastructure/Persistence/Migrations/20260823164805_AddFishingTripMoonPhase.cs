using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishingLog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFishingTripMoonPhase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MoonPhase",
                table: "FishingTrips",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MoonPhase",
                table: "FishingTrips");
        }
    }
}
