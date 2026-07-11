using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrestexaAPI.Migrations
{
    /// <inheritdoc />
    public partial class HardenCompanyDomainValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""CompanyDomains""
                ADD CONSTRAINT ""CK_CompanyDomains_Subdomain_Lowercase""
                CHECK (""Subdomain"" = lower(""Subdomain""));
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""CompanyDomains""
                ADD CONSTRAINT ""CK_CompanyDomains_Subdomain_Format""
                CHECK (""Subdomain"" ~ '^[a-z0-9]+(-[a-z0-9]+)*$');
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""CompanyDomains""
                ADD CONSTRAINT ""CK_CompanyDomains_Subdomain_NotReserved""
                CHECK (lower(""Subdomain"") NOT IN ('admin','api','app','support','auth','system','www','portal','borrower','rea'));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""CompanyDomains""
                DROP CONSTRAINT IF EXISTS ""CK_CompanyDomains_Subdomain_NotReserved"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""CompanyDomains""
                DROP CONSTRAINT IF EXISTS ""CK_CompanyDomains_Subdomain_Format"";
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""CompanyDomains""
                DROP CONSTRAINT IF EXISTS ""CK_CompanyDomains_Subdomain_Lowercase"";
            ");
        }
    }
}
