using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catchlogr.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFishingTripWeather : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AirTemperatureC",
                table: "FishingTrips",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PressureHpa",
                table: "FishingTrips",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeatherCode",
                table: "FishingTrips",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WeatherProvider",
                table: "FishingTrips",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WeatherSampleTimeUtc",
                table: "FishingTrips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WindDirectionDegrees",
                table: "FishingTrips",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WindSpeedMps",
                table: "FishingTrips",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AirTemperatureC",
                table: "FishingTrips");

            migrationBuilder.DropColumn(
                name: "PressureHpa",
                table: "FishingTrips");

            migrationBuilder.DropColumn(
                name: "WeatherCode",
                table: "FishingTrips");

            migrationBuilder.DropColumn(
                name: "WeatherProvider",
                table: "FishingTrips");

            migrationBuilder.DropColumn(
                name: "WeatherSampleTimeUtc",
                table: "FishingTrips");

            migrationBuilder.DropColumn(
                name: "WindDirectionDegrees",
                table: "FishingTrips");

            migrationBuilder.DropColumn(
                name: "WindSpeedMps",
                table: "FishingTrips");
        }
    }
}
