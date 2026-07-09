using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrestexaAPI.Models;

namespace PrestexaAPI.Data.Configurations
{
    public class BorrowerEmploymentConfiguration : IEntityTypeConfiguration<BorrowerEmployment>
    {
        public void Configure(EntityTypeBuilder<BorrowerEmployment> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.BorrowerId, x.EmploymentStatusType });

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Borrower)
                .WithMany()
                .HasForeignKey(x => x.BorrowerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class BorrowerIncomeConfiguration : IEntityTypeConfiguration<BorrowerIncome>
    {
        public void Configure(EntityTypeBuilder<BorrowerIncome> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.BorrowerId, x.IncomeType });

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Borrower)
                .WithMany()
                .HasForeignKey(x => x.BorrowerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.BorrowerEmployment)
                .WithMany()
                .HasForeignKey(x => x.BorrowerEmploymentId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public class BorrowerAssetConfiguration : IEntityTypeConfiguration<BorrowerAsset>
    {
        public void Configure(EntityTypeBuilder<BorrowerAsset> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.BorrowerId, x.AssetType });

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Borrower)
                .WithMany()
                .HasForeignKey(x => x.BorrowerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class BorrowerLiabilityConfiguration : IEntityTypeConfiguration<BorrowerLiability>
    {
        public void Configure(EntityTypeBuilder<BorrowerLiability> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.BorrowerId, x.LiabilityType });

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasPrincipalKey(c => c.NmlsNumber)
                .HasForeignKey(x => x.CompanyNmlsNumber)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Borrower)
                .WithMany()
                .HasForeignKey(x => x.BorrowerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class HousingExpenseConfiguration : IEntityTypeConfiguration<HousingExpense>
    {
        public void Configure(EntityTypeBuilder<HousingExpense> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.LoanId, x.HousingExpenseType });

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
        }
    }

    public class BorrowerDeclarationConfiguration : IEntityTypeConfiguration<BorrowerDeclaration>
    {
        public void Configure(EntityTypeBuilder<BorrowerDeclaration> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.LoanId, x.BorrowerId, x.DeclarationType }).IsUnique();

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
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class GovernmentMonitoringConfiguration : IEntityTypeConfiguration<GovernmentMonitoring>
    {
        public void Configure(EntityTypeBuilder<GovernmentMonitoring> builder)
        {
            builder.HasIndex(x => new { x.CompanyNmlsNumber, x.LoanId, x.BorrowerId }).IsUnique();

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
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
