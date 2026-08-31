using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fluxora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenFinancialInvariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CashMovements_ReferenceType_ReferenceId",
                table: "CashMovements");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReceivableInstallments_Amount_Positive",
                table: "ReceivableInstallments",
                sql: "\"Amount\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReceivableInstallments_AmountPaid_Range",
                table: "ReceivableInstallments",
                sql: "\"AmountPaid\" >= 0 AND \"AmountPaid\" <= \"Amount\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Receipts_Amount_Positive",
                table: "Receipts",
                sql: "\"Amount\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Amount_Positive",
                table: "Payments",
                sql: "\"Amount\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PayableInstallments_Amount_Positive",
                table: "PayableInstallments",
                sql: "\"Amount\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PayableInstallments_AmountPaid_Range",
                table: "PayableInstallments",
                sql: "\"AmountPaid\" >= 0 AND \"AmountPaid\" <= \"Amount\"");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_ReferenceType_ReferenceId",
                table: "CashMovements",
                columns: new[] { "ReferenceType", "ReferenceId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_CashMovements_Amount_Positive",
                table: "CashMovements",
                sql: "\"Amount\" > 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PayableInstallments_PayableInstallmentId",
                table: "Payments",
                column: "PayableInstallmentId",
                principalTable: "PayableInstallments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Payables_PayableId",
                table: "Payments",
                column: "PayableId",
                principalTable: "Payables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_ReceivableInstallments_ReceivableInstallmentId",
                table: "Receipts",
                column: "ReceivableInstallmentId",
                principalTable: "ReceivableInstallments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_Receivables_ReceivableId",
                table: "Receipts",
                column: "ReceivableId",
                principalTable: "Receivables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PayableInstallments_PayableInstallmentId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Payables_PayableId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_ReceivableInstallments_ReceivableInstallmentId",
                table: "Receipts");

            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_Receivables_ReceivableId",
                table: "Receipts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ReceivableInstallments_Amount_Positive",
                table: "ReceivableInstallments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ReceivableInstallments_AmountPaid_Range",
                table: "ReceivableInstallments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Receipts_Amount_Positive",
                table: "Receipts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Amount_Positive",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PayableInstallments_Amount_Positive",
                table: "PayableInstallments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PayableInstallments_AmountPaid_Range",
                table: "PayableInstallments");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_ReferenceType_ReferenceId",
                table: "CashMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CashMovements_Amount_Positive",
                table: "CashMovements");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_ReferenceType_ReferenceId",
                table: "CashMovements",
                columns: new[] { "ReferenceType", "ReferenceId" });
        }
    }
}
