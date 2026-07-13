using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrestexaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminSettingsPhase1B : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutomationRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TriggerType = table.Column<string>(type: "text", nullable: true),
                    ActionType = table.Column<string>(type: "text", nullable: true),
                    TriggerJson = table.Column<string>(type: "jsonb", nullable: true),
                    ActionJson = table.Column<string>(type: "jsonb", nullable: true),
                    Milestone = table.Column<string>(type: "text", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomationRules_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientNeedsRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TriggerEvent = table.Column<string>(type: "text", nullable: true),
                    ConditionJson = table.Column<string>(type: "jsonb", nullable: true),
                    RequestedDocumentsJson = table.Column<string>(type: "jsonb", nullable: true),
                    TargetRecipientType = table.Column<string>(type: "text", nullable: true),
                    Milestone = table.Column<string>(type: "text", nullable: true),
                    ReminderEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientNeedsRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientNeedsRules_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClosingCostItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", nullable: false),
                    FeeName = table.Column<string>(type: "text", nullable: false),
                    FeeCategory = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    PaidBy = table.Column<string>(type: "text", nullable: true),
                    IsFinanceCharge = table.Column<bool>(type: "boolean", nullable: false),
                    IsAprFee = table.Column<bool>(type: "boolean", nullable: false),
                    StateApplicability = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClosingCostItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClosingCostItems_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    FormType = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<string>(type: "text", nullable: true),
                    OperationalAssetId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormDefinitions_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormDefinitions_OperationalAssets_OperationalAssetId",
                        column: x => x.OperationalAssetId,
                        principalTable: "OperationalAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FormSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormSets_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormSetItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FormSetId = table.Column<int>(type: "integer", nullable: false),
                    FormDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSetItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormSetItems_FormDefinitions_FormDefinitionId",
                        column: x => x.FormDefinitionId,
                        principalTable: "FormDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormSetItems_FormSets_FormSetId",
                        column: x => x.FormSetId,
                        principalTable: "FormSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationRules_CompanyNmlsNumber_Name",
                table: "AutomationRules",
                columns: new[] { "CompanyNmlsNumber", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientNeedsRules_CompanyNmlsNumber_Name",
                table: "ClientNeedsRules",
                columns: new[] { "CompanyNmlsNumber", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClosingCostItems_CompanyNmlsNumber_DisplayOrder",
                table: "ClosingCostItems",
                columns: new[] { "CompanyNmlsNumber", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ClosingCostItems_CompanyNmlsNumber_FeeName",
                table: "ClosingCostItems",
                columns: new[] { "CompanyNmlsNumber", "FeeName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormDefinitions_CompanyNmlsNumber_Category",
                table: "FormDefinitions",
                columns: new[] { "CompanyNmlsNumber", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_FormDefinitions_CompanyNmlsNumber_Name",
                table: "FormDefinitions",
                columns: new[] { "CompanyNmlsNumber", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormDefinitions_OperationalAssetId",
                table: "FormDefinitions",
                column: "OperationalAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSetItems_FormDefinitionId",
                table: "FormSetItems",
                column: "FormDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSetItems_FormSetId_DisplayOrder",
                table: "FormSetItems",
                columns: new[] { "FormSetId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FormSetItems_FormSetId_FormDefinitionId",
                table: "FormSetItems",
                columns: new[] { "FormSetId", "FormDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormSets_CompanyNmlsNumber_Name",
                table: "FormSets",
                columns: new[] { "CompanyNmlsNumber", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutomationRules");

            migrationBuilder.DropTable(
                name: "ClientNeedsRules");

            migrationBuilder.DropTable(
                name: "ClosingCostItems");

            migrationBuilder.DropTable(
                name: "FormSetItems");

            migrationBuilder.DropTable(
                name: "FormDefinitions");

            migrationBuilder.DropTable(
                name: "FormSets");
        }
    }
}
