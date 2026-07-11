using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrestexaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditReportsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CreditReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LoanId = table.Column<int>(type: "integer", nullable: false),
                    BorrowerId = table.Column<int>(type: "integer", nullable: true),
                    CoBorrowerId = table.Column<int>(type: "integer", nullable: true),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReportType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OrderedByUserId = table.Column<int>(type: "integer", nullable: false),
                    OrderedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TransUnionScore = table.Column<int>(type: "integer", nullable: true),
                    EquifaxScore = table.Column<int>(type: "integer", nullable: true),
                    ExperianScore = table.Column<int>(type: "integer", nullable: true),
                    MiddleScore = table.Column<int>(type: "integer", nullable: true),
                    RawDataLocation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    XmlFileLocation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PdfFileLocation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditReports_Borrowers_BorrowerId",
                        column: x => x.BorrowerId,
                        principalTable: "Borrowers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CreditReports_Borrowers_CoBorrowerId",
                        column: x => x.CoBorrowerId,
                        principalTable: "Borrowers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CreditReports_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditReports_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditReports_Users_OrderedByUserId",
                        column: x => x.OrderedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditReports_BorrowerId",
                table: "CreditReports",
                column: "BorrowerId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditReports_CoBorrowerId",
                table: "CreditReports",
                column: "CoBorrowerId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditReports_CompanyNmlsNumber_LoanId_OrderedAtUtc",
                table: "CreditReports",
                columns: new[] { "CompanyNmlsNumber", "LoanId", "OrderedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditReports_CompanyNmlsNumber_LoanId_Status",
                table: "CreditReports",
                columns: new[] { "CompanyNmlsNumber", "LoanId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditReports_LoanId",
                table: "CreditReports",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditReports_OrderedByUserId",
                table: "CreditReports",
                column: "OrderedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditReports");
        }
    }
}
