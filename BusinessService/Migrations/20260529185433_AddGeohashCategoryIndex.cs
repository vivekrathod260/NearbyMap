using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusinessService.Migrations
{
    /// <inheritdoc />
    public partial class AddGeohashCategoryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BusinessLocation_Geohash_Category",
                table: "Businesses",
                columns: new[] { "Geohash", "Category" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BusinessLocation_Geohash_Category",
                table: "Businesses");
        }
    }
}
