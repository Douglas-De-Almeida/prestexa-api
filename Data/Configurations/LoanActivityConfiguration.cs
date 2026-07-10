using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrestexaAPI.Models;

namespace PrestexaAPI.Data.Configurations
{
    public class LoanActivityConfiguration : IEntityTypeConfiguration<LoanActivity>
    {
        public void Configure(EntityTypeBuilder<LoanActivity> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.LoanId, x.CreatedAtUtc });
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.LoanId, x.ParentActivityId });

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Loan)
                .WithMany()
                .HasForeignKey(x => x.LoanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.ParentActivity)
                .WithMany()
                .HasForeignKey(x => x.ParentActivityId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class LoanActivityAttachmentConfiguration : IEntityTypeConfiguration<LoanActivityAttachment>
    {
        public void Configure(EntityTypeBuilder<LoanActivityAttachment> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.LoanId, x.LoanActivityId, x.UploadedAtUtc });

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Loan)
                .WithMany()
                .HasForeignKey(x => x.LoanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.LoanActivity)
                .WithMany()
                .HasForeignKey(x => x.LoanActivityId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UploadedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
