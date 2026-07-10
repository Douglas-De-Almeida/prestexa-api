using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrestexaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoanActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LoanId = table.Column<int>(type: "integer", nullable: false),
                    LoanNumber = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ParentActivityId = table.Column<int>(type: "integer", nullable: true),
                    ActivityType = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    NotifyLoanTeam = table.Column<bool>(type: "boolean", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    ActorUserId = table.Column<int>(type: "integer", nullable: true),
                    ActorName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ActorRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ActorType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanActivities_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanActivities_LoanActivities_ParentActivityId",
                        column: x => x.ParentActivityId,
                        principalTable: "LoanActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoanActivities_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoanActivityAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LoanId = table.Column<int>(type: "integer", nullable: false),
                    LoanActivityId = table.Column<int>(type: "integer", nullable: false),
                    AttachmentType = table.Column<int>(type: "integer", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StoredFilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ThumbnailFilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UploadedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanActivityAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanActivityAttachments_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanActivityAttachments_LoanActivities_LoanActivityId",
                        column: x => x.LoanActivityId,
                        principalTable: "LoanActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoanActivityAttachments_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoanActivityAttachments_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoanActivities_CompanyNmlsNumber_LoanId_CreatedAtUtc",
                table: "LoanActivities",
                columns: new[] { "CompanyNmlsNumber", "LoanId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanActivities_CompanyNmlsNumber_LoanId_ParentActivityId",
                table: "LoanActivities",
                columns: new[] { "CompanyNmlsNumber", "LoanId", "ParentActivityId" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanActivities_LoanId",
                table: "LoanActivities",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanActivities_ParentActivityId",
                table: "LoanActivities",
                column: "ParentActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanActivityAttachments_CompanyNmlsNumber_LoanId_LoanActivi~",
                table: "LoanActivityAttachments",
                columns: new[] { "CompanyNmlsNumber", "LoanId", "LoanActivityId", "UploadedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanActivityAttachments_LoanActivityId",
                table: "LoanActivityAttachments",
                column: "LoanActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanActivityAttachments_LoanId",
                table: "LoanActivityAttachments",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanActivityAttachments_UploadedByUserId",
                table: "LoanActivityAttachments",
                column: "UploadedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoanActivityAttachments");

            migrationBuilder.DropTable(
                name: "LoanActivities");
        }
    }
}
