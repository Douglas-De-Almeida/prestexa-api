using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrestexaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoginStates",
                columns: table => new
                {
                    StateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SourceHost = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TenantSlug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginStates", x => x.StateId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginStates");
        }
    }
}
