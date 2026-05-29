using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProximityService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessLocations",
                columns: table => new
                {
                    BusinessId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    Geohash = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessLocations", x => x.BusinessId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessLocation_Geohash",
                table: "BusinessLocations",
                column: "Geohash");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessLocation_Geohash_Category",
                table: "BusinessLocations",
                columns: new[] { "Geohash", "Category" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessLocations");
        }
    }
}
