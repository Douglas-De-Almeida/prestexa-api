using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrestexaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaAssetsAndCompanyBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoragePath = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyBrandings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: false),
                    LightLogoAssetId = table.Column<int>(type: "integer", nullable: true),
                    DarkLogoAssetId = table.Column<int>(type: "integer", nullable: true),
                    BackgroundAssetId = table.Column<int>(type: "integer", nullable: true),
                    PrimaryColor = table.Column<string>(type: "text", nullable: false),
                    SecondaryColor = table.Column<string>(type: "text", nullable: false),
                    AccentColor = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyBrandings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyBrandings_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyBrandings_MediaAssets_BackgroundAssetId",
                        column: x => x.BackgroundAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyBrandings_MediaAssets_DarkLogoAssetId",
                        column: x => x.DarkLogoAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyBrandings_MediaAssets_LightLogoAssetId",
                        column: x => x.LightLogoAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBrandings_BackgroundAssetId",
                table: "CompanyBrandings",
                column: "BackgroundAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBrandings_CompanyNmlsNumber",
                table: "CompanyBrandings",
                column: "CompanyNmlsNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBrandings_DarkLogoAssetId",
                table: "CompanyBrandings",
                column: "DarkLogoAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBrandings_LightLogoAssetId",
                table: "CompanyBrandings",
                column: "LightLogoAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_PublicId",
                table: "MediaAssets",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyBrandings");

            migrationBuilder.DropTable(
                name: "MediaAssets");
        }
    }
}
