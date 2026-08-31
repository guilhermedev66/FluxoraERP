using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fluxora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CorrectReportingAggregates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LineTotal",
                table: "SalesOrderLines",
                type: "numeric(19,2)",
                precision: 19,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LineTotal",
                table: "PurchaseOrderLines",
                type: "numeric(19,2)",
                precision: 19,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE "SalesOrderLines"
                SET "LineTotal" = ROUND("Quantity" * "UnitPrice", 2);

                UPDATE "PurchaseOrderLines"
                SET "LineTotal" = ROUND("Quantity" * "UnitPrice", 2);

                UPDATE "SalesOrders" AS orders
                SET "Total" = COALESCE((
                    SELECT SUM(lines."LineTotal")
                    FROM "SalesOrderLines" AS lines
                    WHERE lines."SalesOrderId" = orders."Id"
                ), 0);

                UPDATE "PurchaseOrders" AS orders
                SET "Total" = COALESCE((
                    SELECT SUM(lines."LineTotal")
                    FROM "PurchaseOrderLines" AS lines
                    WHERE lines."PurchaseOrderId" = orders."Id"
                ), 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LineTotal",
                table: "SalesOrderLines");

            migrationBuilder.DropColumn(
                name: "LineTotal",
                table: "PurchaseOrderLines");
        }
    }
}
