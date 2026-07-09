using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrestexaAPI.Models;

namespace PrestexaAPI.Data.Configurations
{
    public class MismoImportRecordConfiguration : IEntityTypeConfiguration<MismoImportRecord>
    {
        public void Configure(EntityTypeBuilder<MismoImportRecord> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.ContentSha256 }).IsUnique();
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.LoanId });

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Loan)
                .WithMany()
                .HasForeignKey(x => x.LoanId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
