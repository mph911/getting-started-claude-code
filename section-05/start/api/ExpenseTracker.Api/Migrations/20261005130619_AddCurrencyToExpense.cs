using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencyToExpense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Expenses",
                type: "character(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");

            // The constant default backfills existing rows (metadata-only on Postgres 11+): every
            // expense recorded before multi-currency support was implicitly EUR. Drop it afterwards;
            // the API always supplies the currency.
            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Expenses",
                type: "character(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character(3)",
                oldFixedLength: true,
                oldMaxLength: 3,
                oldDefaultValue: "EUR");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Expenses");
        }
    }
}
