using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishingLog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Catches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    Species = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Length = table.Column<int>(type: "integer", nullable: true),
                    Weight = table.Column<int>(type: "integer", nullable: true),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CaughtAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Depth = table.Column<double>(type: "double precision", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Bait_Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Bait_Type = table.Column<int>(type: "integer", nullable: true),
                    Bait_Color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Bait_WeightGrams = table.Column<int>(type: "integer", nullable: true),
                    Bait_LengthMm = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Catches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Catches_FishingTrips_TripId",
                        column: x => x.TripId,
                        principalTable: "FishingTrips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Catches_LastModified",
                table: "Catches",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_Catches_TripId",
                table: "Catches",
                column: "TripId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Catches");
        }
    }
}
