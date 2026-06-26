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
            // Add nullable first so existing rows can be backfilled, then make NOT NULL.
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Expenses",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Expenses\" SET \"Currency\" = 'EUR';");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Expenses",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldNullable: true);
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
