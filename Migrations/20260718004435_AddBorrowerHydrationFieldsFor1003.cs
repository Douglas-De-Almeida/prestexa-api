using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrestexaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddBorrowerHydrationFieldsFor1003 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LanguagePreferences",
                table: "Borrowers",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MailingAddressSameAsCurrent",
                table: "Borrowers",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherLanguageDescription",
                table: "Borrowers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkPhoneExtension",
                table: "Borrowers",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MailingAddressSameAsCurrent",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "OtherLanguageDescription",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "WorkPhoneExtension",
                table: "Borrowers");

            migrationBuilder.AlterColumn<string>(
                name: "LanguagePreferences",
                table: "Borrowers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
