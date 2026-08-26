using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishingLog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivateCatchPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatchPhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatchPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatchPhotos_Catches_CatchId",
                        column: x => x.CatchId,
                        principalTable: "Catches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatchPhotos_CatchId",
                table: "CatchPhotos",
                column: "CatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatchPhotos_StorageKey",
                table: "CatchPhotos",
                column: "StorageKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatchPhotos");
        }
    }
}
