using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrestexaAPI.Migrations
{
    public partial class DisableTrustedDeviceEnforcement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""Users""
                SET ""RestrictLoginToApprovedDevices"" = FALSE
                WHERE ""RestrictLoginToApprovedDevices"" = TRUE;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""OrganizationAuditRecords""
                (
                    ""OrganizationId"",
                    ""CompanyNmlsNumber"",
                    ""Action"",
                    ""FieldName"",
                    ""OldValue"",
                    ""NewValue"",
                    ""ChangedAtUtc""
                )
                SELECT
                    c.""Id"",
                    c.""NmlsNumber"",
                    'TrustedDeviceEnforcementDisabled',
                    'RestrictLoginToApprovedDevices',
                    'true',
                    'false',
                    NOW()
                FROM ""Companies"" c;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}