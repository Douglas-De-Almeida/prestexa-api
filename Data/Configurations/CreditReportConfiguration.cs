using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrestexaAPI.Models;

namespace PrestexaAPI.Data.Configurations
{
    public class CreditReportConfiguration : IEntityTypeConfiguration<CreditReport>
    {
        public void Configure(EntityTypeBuilder<CreditReport> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.LoanId, x.OrderedAtUtc });
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.LoanId, x.Status });

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Loan)
                .WithMany()
                .HasForeignKey(x => x.LoanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Borrower)
                .WithMany()
                .HasForeignKey(x => x.BorrowerId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.CoBorrower)
                .WithMany()
                .HasForeignKey(x => x.CoBorrowerId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(x => x.OrderedByUser)
                .WithMany()
                .HasForeignKey(x => x.OrderedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
