using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fluxora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SnapshotPurchaseOrderLineCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductCategory",
                table: "PurchaseOrderLines",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Existing lines have no historical category snapshot. Seed them from the current
            // catalog value; changes that happened before this migration cannot be reconstructed.
            migrationBuilder.Sql(
                """
                UPDATE "PurchaseOrderLines" AS line
                SET "ProductCategory" = product."Category"
                FROM "Products" AS product
                WHERE line."ProductId" = product."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductCategory",
                table: "PurchaseOrderLines");
        }
    }
}
