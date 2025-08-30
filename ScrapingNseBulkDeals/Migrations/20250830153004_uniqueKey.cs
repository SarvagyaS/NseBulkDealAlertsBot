using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScrapingNseBulkDeals.Migrations
{
    /// <inheritdoc />
    public partial class uniqueKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Deals_TradedDate_SecurityName_ClientName_DealType_Quantity_Price_Symbol",
                table: "Deals");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
