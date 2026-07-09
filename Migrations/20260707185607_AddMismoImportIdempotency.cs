using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrestexaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMismoImportIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MismoImportRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContentSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LoanId = table.Column<int>(type: "integer", nullable: false),
                    ImportedByUserId = table.Column<int>(type: "integer", nullable: false),
                    SourceMismoFileId = table.Column<int>(type: "integer", nullable: true),
                    MismoVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ImportedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MismoImportRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MismoImportRecords_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MismoImportRecords_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MismoImportRecords_CompanyNmlsNumber_ContentSha256",
                table: "MismoImportRecords",
                columns: new[] { "CompanyNmlsNumber", "ContentSha256" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MismoImportRecords_CompanyNmlsNumber_LoanId",
                table: "MismoImportRecords",
                columns: new[] { "CompanyNmlsNumber", "LoanId" });

            migrationBuilder.CreateIndex(
                name: "IX_MismoImportRecords_LoanId",
                table: "MismoImportRecords",
                column: "LoanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MismoImportRecords");
        }
    }
}
